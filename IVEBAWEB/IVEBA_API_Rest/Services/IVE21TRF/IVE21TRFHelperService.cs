using IVEBA_API_Rest.Helpers;
using IVEBA_API_Rest.Models.IVE17DV;
using IVEBA_API_Rest.Models.IVE21TRF;
using IVEBA_API_Rest.Models.IVECH;
using IVEBA_API_Rest.Utilidades;
using System.Data;
using System.Data.SqlClient;
using System.Numerics;
using System.Runtime.Serialization.Formatters;
using System.Text;

namespace IVEBA_API_Rest.Services.IVE21TRF
{
    public class IVE21TRFHelperService : IIVE21TRFHelperService
    {
        private readonly DbHelper _dbHelper;
        private readonly UtilidadesAPP utilidades;

        private int contadorNit = 0;
        private int cantidadRegsDetalleOK = 0;
        private int cantidadRegsDetalleERROR = 0;
        public IVE21TRFHelperService(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
            utilidades = new UtilidadesAPP();
        }

        public async Task<DTO_IVE21TRFResponse> GeneracionArchivoIVE21TRF(int fechaInicial, int fechaFinal, bool archivoDefinitivo)
        {
            DTO_IVE21TRFResponse response = new DTO_IVE21TRFResponse();
            string filePath = Path.Combine(Path.GetTempPath(), "archivoGenerado.txt");
            string datosPersona = "";
            string datosEmpresa = "";
            int cantidadRegistrosOK = 0;
            StringBuilder logErrores = new StringBuilder();

            try
            {
                // Verificar si se debe generar el archivo definitivo
                if (archivoDefinitivo)
                {
                    List<DTO_IVETRF21Archivos> registros = ConsultarIVETRF21PorRangoFechas(fechaInicial, fechaFinal);

                    // Verifica si hay un archivo existente con la fecha enviada, de ser asi, devuelve la información ya existente para no volver a generarla.
                    if (registros.Count > 0)
                    {
                        //Existe archivo, devuelve archivo generado.
                        using (StreamWriter archivo = new StreamWriter(filePath))
                        {
                            foreach (var registro in registros)
                            {
                                string stringGrabar = $"{registro.String}";
                                stringGrabar = utilidades.QuitoTildes(stringGrabar);
                                archivo.WriteLine(stringGrabar);
                                cantidadRegistrosOK++;
                            }
                        }
                        byte[] fileBytesExistente = File.ReadAllBytes(filePath);
                        File.Delete(filePath);
                        response.registrosOKEncabezado = cantidadRegistrosOK;
                        response.registrosErrorEncabezado = 0;
                        response.archivoTXTOk = fileBytesExistente;
                        return response;
                    }
                    // Genera nueva información
                    else
                    {
                        //No existe archivo, genera uno nuevo
                        TruncaIVETRF21Temporal();
                        List<DTO_IVETRF21Clientes> listaClientes = ConsultarIVETRF21ClientesPorFecha(fechaInicial, fechaFinal);
                        foreach (DTO_IVETRF21Clientes cliente in listaClientes)
                        {
                            switch (cliente.Tipo)
                            {
                                case 2:
                                case 3:
                                    if (ProcesoFisicos(cliente.Cliente, out datosPersona))
                                    {
                                        InsertarCHCajaTemporal(cliente.Cliente, datosPersona, 0, 0, 0);
                                        response.registrosOKEncabezado++;
                                    }
                                    else
                                    {
                                        InsertarCHCajaTemporal(cliente.Cliente, "ERROR", 99, 99, 9999);
                                        logErrores.AppendLine($"{cliente.Cliente.ToString("D12")} {cliente.Nombre} Error al procesar físico");
                                        response.registrosOKEncabezado++;
                                    }
                                    break;
                                case 4:
                                case 1:
                                    if (ProcesoJuridicos(cliente.Cliente, out datosEmpresa))
                                    {
                                        datosEmpresa = datosEmpresa.Replace("'", " ");
                                        InsertarCHCajaTemporal(cliente.Cliente, datosEmpresa, 0, 0, 0);
                                        response.registrosOKEncabezado++;
                                    }
                                    else
                                    {
                                        InsertarCHCajaTemporal(cliente.Cliente, "ERROR", 99, 99, 9999);
                                        logErrores.AppendLine($"{cliente.Cliente.ToString("D12")} {cliente.Nombre} Error al procesar jurídico");
                                        response.registrosOKEncabezado++;
                                    }
                                    break;
                            }
                        }
                    }
                }

                // *****************************
                // ARCHIVO CON ERRORES 
                // *****************************
                await File.WriteAllTextAsync(filePath, logErrores.ToString());
                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                File.Delete(filePath);
                response.archivoTXTErrores = fileBytes;

                // *****************************
                // ARCHIVO OK
                // *****************************
                response.archivoTXTOk = TransaccionesClientes(archivoDefinitivo, filePath, fechaInicial, fechaFinal);


                response.cantidadNit = contadorNit;
                response.registrosOKDetalle = cantidadRegsDetalleOK;
                response.registrosERRORDetalle = cantidadRegsDetalleERROR;
            }
            catch (Exception ex)
            {
                throw new Exception("Error en GeneracionArchivoIVE21TRF : " + ex.Message);
            }
            return response;

        }

        private bool ProcesoFisicos(int cliente, out string stringDatos)
        {
            try
            {
                stringDatos = string.Empty;
                List<DTO_DWCliente> clientes = ConsultarDWCliente(cliente);

                if (clientes.Count == 0)
                    return false;

                var clienteData = clientes.First(); // Suponiendo un solo cliente por ID
                var stringArmado = "I";
                string orden = clienteData.Identificacion.Substring(1, 3);

                // Construcción de identificación y tipo de documento
                switch (clienteData.TipoIdentificacion)
                {
                    case 1: // Cedula

                        if (orden[1] == '0')
                        {
                            orden = orden[0] + orden[2].ToString();
                        }
                        orden = utilidades.FormateoString2(orden, 3, ' ', true);
                        stringArmado += "C" + orden + utilidades.FormateoString2(clienteData.Identificacion.Substring(5, 7), 20, ' ', true);
                        break;

                    case 2: // Partida
                        orden = utilidades.FormateoString2(orden, 3, ' ', true);
                        stringArmado += "O" + orden + utilidades.FormateoString2(" ", 3, ' ', true) + utilidades.FormateoString2(clienteData.Identificacion, 20, ' ', true);
                        break;

                    case 4: // Pasaporte
                        orden = utilidades.FormateoString2(orden, 3, ' ', true);
                        stringArmado += "P" + orden + utilidades.FormateoString2(" ", 3, ' ', true) + utilidades.FormateoString2(clienteData.Identificacion, 20, ' ', true);
                        break;

                    case 26: // DPI
                        orden = "   ";
                        stringArmado += "D" + orden + utilidades.FormateoString2(clienteData.Identificacion, 20, ' ', true);
                        break;
                }

                // Apellidos y nombres con tildes removidas y en mayúsculas
                stringArmado += utilidades.FormateoString2(utilidades.QuitoTildes(clienteData.Apellido1.ToUpper()), 15, ' ', true);
                stringArmado += utilidades.FormateoString2(utilidades.QuitoTildes(clienteData.Apellido2.ToUpper()), 15, ' ', true);
                stringArmado += utilidades.FormateoString2(utilidades.QuitoTildes(clienteData.ApellidoCasada.ToUpper()), 15, ' ', true);
                stringArmado += utilidades.FormateoString2(utilidades.QuitoTildes(clienteData.Nombre1.ToUpper()), 15, ' ', true);
                stringArmado += utilidades.FormateoString2(utilidades.QuitoTildes(clienteData.Nombre2.ToUpper()), 15, ' ', true);

                stringDatos = stringArmado;
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(" Error en ProcesoFisicos " + ex.Message);
            }

        }

        private bool ProcesoJuridicos(int cliente, out string stringDatos)
        {
            try
            {
                stringDatos = string.Empty;
                var clientes = ConsultarDWCliente(cliente);

                if (clientes.Count == 0)
                    return false;

                var clienteData = clientes.First(); // Suponiendo un solo cliente por ID
                var stringArmado = "J";

                // Determina el NIT de la empresa
                string nitEmpresa = string.IsNullOrEmpty(clienteData.Nit) || clienteData.Nit == "0"
                    ? clienteData.TipoIdentificacion == 8 ? clienteData.Identificacion : "SINNIT"
                    : clienteData.Nit;

                // Incrementa el contador si NitEmpresa es "SINNIT"
                if (nitEmpresa == "SINNIT")
                {
                    contadorNit++;
                }

                // Asigna un NIT específico si es el cliente con CodCliente "10"
                if (clienteData.CodCliente == 10) nitEmpresa = "1205544";

                // Construcción del StringArmado
                stringArmado += "N   " + utilidades.FormateoString2(utilidades.QuitoCaracter(nitEmpresa), 20, ' ', true);
                stringArmado += utilidades.FormateoString2(utilidades.QuitoTildes(clienteData.NombreCliente.ToUpper()), 75, ' ', true);

                stringDatos = stringArmado;
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(" Error en ProcesoJuridicos " + ex.Message);
            }
        }

        private byte[] TransaccionesClientes(bool tipoArchivo, string filePath, int fechaInicio, int fechaFin)
        {
            string StringGrabar = "";
            bool CltOrd;
            try
            {
                List<DTO_IVE21TRF> listaIVE21TRF = ConsultarIVE21TRFPorFecha(fechaInicio, fechaFin);
                using (StreamWriter fileWriter = new StreamWriter(filePath, append: false))
                {
                    foreach (DTO_IVE21TRF registro in listaIVE21TRF)
                    {
                        switch (registro.TRFTIPO)
                        {
                            case "2":
                                if (registro.TRFOCUN.Equals("0") || registro.TRFOCUN.Trim().Equals(""))
                                {
                                    CltOrd = false;
                                    List<DTO_IVETRF21Temporal> listaIVE21Temporal = ConsultarIVETRF21Temporal(registro.TRFOCUN);
                                    foreach (DTO_IVETRF21Temporal registro2 in listaIVE21Temporal)
                                    {
                                        StringGrabar = "";
                                        StringGrabar += registro.TRFFECHA.ToString("yyyyMMdd") + "&&";
                                        StringGrabar += "2" + "&&";
                                        StringGrabar += "E" + "&&";

                                        if (registro2.String.Substring(0, 1) == "I")
                                        {
                                            StringGrabar += utilidades.FormateoString(registro2.String.Trim(), 135, " ", "I") + "&&";
                                        }
                                        else
                                        {
                                            StringGrabar += utilidades.FormateoString(registro2.String.Trim(), 135, " ", "I") + "&&";
                                        }
                                        CltOrd = true;
                                        fileWriter.WriteLine(StringGrabar);
                                    }
                                }
                                else
                                {
                                    CltOrd = false;
                                }

                                if (!CltOrd)
                                {
                                    StringGrabar = "";
                                    StringGrabar += registro.TRFFECHA.ToString("yyyyMMdd") + "&&";
                                    StringGrabar += "2" + "&&";
                                    StringGrabar += "E" + "&&";

                                    string OTIPOP = registro.TRFOTPER.Trim();

                                    if (OTIPOP == "I")
                                    {
                                        string OTIPOID = registro.TRFOTID.Trim();
                                        string OORDEN = OTIPOID == "C" ? registro.TRFOORD.Trim() : "   ";
                                        string OID = registro.TRFODOC.Trim();
                                        string OMUNI = OTIPOID == "C" ? registro.TRFOMUN.Trim() : "  ";
                                        string OPAPE = registro.TRFOAPE1.Trim();
                                        string OSAPE = registro.TRFOAPE2.Trim();
                                        string OACAS = registro.TRFOAPEC.Trim();
                                        string OPNOM = registro.TRFONOM1.Trim();
                                        string OSNOM = registro.TRFONOM2.Trim();

                                        StringGrabar += utilidades.FormateoString(OTIPOP, 1, " ", "I") + "&&";
                                        StringGrabar += utilidades.FormateoString(OTIPOID, 1, " ", "I") + "&&";
                                        StringGrabar += utilidades.FormateoString(OORDEN, 3, " ", "I") + "&&";
                                        StringGrabar += utilidades.FormateoString(OID, 20, " ", "I") + "&&";
                                        StringGrabar += utilidades.FormateoString(OMUNI, 2, " ", "I") + "&&";
                                        StringGrabar += utilidades.FormateoString(OPAPE, 15, " ", "I") + "&&";
                                        StringGrabar += utilidades.FormateoString(OSAPE, 15, " ", "I") + "&&";
                                        StringGrabar += utilidades.FormateoString(OACAS, 15, " ", "I") + "&&";
                                        StringGrabar += utilidades.FormateoString(OPNOM, 15, " ", "I") + "&&";
                                        StringGrabar += utilidades.FormateoString(OSNOM, 30, " ", "I") + "&&";
                                    }
                                    else
                                    {
                                        string OTIPOID = registro.TRFOTID.Trim();
                                        string OORDEN = "   ";
                                        string OID = registro.TRFODOC.Trim();
                                        string OMUNI = "  ";
                                        string OPAPE = registro.TRFOAPE1.Trim();
                                        OTIPOP = "";

                                        StringGrabar += utilidades.FormateoString(OTIPOP, 1, " ", "I") + "&&";
                                        StringGrabar += utilidades.FormateoString(OTIPOID, 1, " ", "I") + "&&";
                                        StringGrabar += utilidades.FormateoString(OORDEN, 3, " ", "I") + "&&";
                                        StringGrabar += utilidades.FormateoString(OID, 20, " ", "I") + "&&";
                                        StringGrabar += utilidades.FormateoString(OMUNI, 2, " ", "I") + "&&";
                                        StringGrabar += utilidades.FormateoString(utilidades.QuitoTildes(OPAPE.Substring(0, 15)), 15, " ", 1) + "&&";
                                        StringGrabar += utilidades.FormateoString(utilidades.QuitoTildes(OPAPE.Substring(15, 15)), 15, " ", 1) + "&&";
                                        StringGrabar += utilidades.FormateoString(utilidades.QuitoTildes(OPAPE.Substring(30, 15)), 15, " ", 1) + "&&";
                                        StringGrabar += utilidades.FormateoString(utilidades.QuitoTildes(OPAPE.Substring(45, 15)), 15, " ", 1) + "&&";
                                        StringGrabar += utilidades.FormateoString(utilidades.QuitoTildes(OPAPE.Substring(60, 30)), 30, " ", 1) + "&&";
                                    }                                                                        
                                }

                                fileWriter.WriteLine(StringGrabar);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return null;
        }



        private void TruncaIVETRF21Temporal()
        {
            string query = "Truncate table IVE_TRF21_Temporal";
            try
            {
                _dbHelper.ExecuteNonQuery(query);
            }
            catch (Exception ex)
            {
                throw new Exception(" Error al truncar TruncaIVETRF21Temporal " + ex.Message);
            }
        }
        private int InsertarCHCajaTemporal(int cliente, string cadena, int dia, int mes, int ano)
        {
            string query = $"INSERT into IVE_TRF21_TEMPORAL Values ({cliente}, '{cadena}', {dia}, {mes}, {ano} );";
            int filasAfectadas = 0;
            try
            {
                filasAfectadas = _dbHelper.ExecuteNonQuery(query);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al insertar InsertarCHCajaTemporal " + ex.Message);
            }

            return filasAfectadas;
        }

        private List<DTO_IVETRF21Temporal> ConsultarIVETRF21Temporal(string cliente)
        {
            List<DTO_IVETRF21Temporal> listaDatos = new List<DTO_IVETRF21Temporal>();

            string query = "SELECT * FROM IVE_TRF21_TEMPORAL WHERE Cliente = @Cliente";

            SqlParameter[] parameters = {
                new SqlParameter("@Cliente", cliente)
            };

            try
            {
                DataTable dt = _dbHelper.ExecuteSelectCommand(query, parameters);

                foreach (DataRow row in dt.Rows)
                {
                    listaDatos.Add(new DTO_IVETRF21Temporal
                    {
                        Cliente = Convert.ToSingle(row["Cliente"]),
                        String = row["String"].ToString(),
                        Dia = Convert.ToInt32(row["Dia"]),
                        Mes = Convert.ToInt32(row["Mes"]),
                        Ano = Convert.ToInt32(row["Ano"])
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error en ConsultarIVETRF21Temporal: " + ex.Message);
            }

            return listaDatos;
        }
        private List<DTO_IVETRF21Archivos> ConsultarIVETRF21PorRangoFechas(int fechaInicial, int fechaFinal)
        {
            List<DTO_IVETRF21Archivos> listaDatos = new List<DTO_IVETRF21Archivos>();

            // Extraer año y mes de las fechas inicial y final
            int anioInicial = fechaInicial / 10000;
            int mesInicial = (fechaInicial / 100) % 100;
            int anioFinal = fechaFinal / 10000;
            int mesFinal = (fechaFinal / 100) % 100;

            string query = @"
                SELECT * 
                FROM IVE_TRF21_Archivos 
                WHERE (Ano > @AnioInicial OR (Ano = @AnioInicial AND Mes >= @MesInicial))
                  AND (Ano < @AnioFinal OR (Ano = @AnioFinal AND Mes <= @MesFinal))";

            SqlParameter[] parameters = {
                        new SqlParameter("@AnioInicial", anioInicial),
                        new SqlParameter("@MesInicial", mesInicial),
                        new SqlParameter("@AnioFinal", anioFinal),
                        new SqlParameter("@MesFinal", mesFinal)
                    };

            try
            {
                DataTable dt = _dbHelper.ExecuteSelectCommand(query, parameters);

                foreach (DataRow row in dt.Rows)
                {
                    listaDatos.Add(new DTO_IVETRF21Archivos
                    {
                        Fecha = row["Fecha"].ToString(),
                        Archivo = row["Archivo"].ToString()[0], // Convertir a char
                        Ordinal = Convert.ToInt32(row["Ordinal"]),
                        Mes = Convert.ToInt32(row["Mes"]),
                        Ano = Convert.ToInt32(row["Ano"]),
                        String = row["String"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error en ConsultarIVETRF21PorRangoFechas: " + ex.Message);
            }

            return listaDatos;
        }
        private List<DTO_IVETRF21Clientes> ConsultarIVETRF21ClientesPorFecha(int anio, int mes)
        {
            List<DTO_IVETRF21Clientes> listaClientes = new List<DTO_IVETRF21Clientes>();
            string query = @"
                SELECT DISTINCT 
                    Cliente, 
                    NombreCliente AS NombreCliente, 
                    TipoCliente AS TipoCliente
                FROM (
                    SELECT 
                        trfocun AS Cliente
                    FROM 
                        ive21transferencia
                    WHERE 
                        ive21 = 'S' 
                        AND trfocun <> '' 
                        AND trfocun <> '0' 
                        AND YEAR(TRFFECHA) = @Anio 
                        AND MONTH(TRFFECHA) = @Mes

                    UNION ALL

                    SELECT 
                        trfbcun AS Cliente
                    FROM 
                        ive21transferencia
                    WHERE 
                        ive21 = 'S' 
                        AND trfbcun <> '' 
                        AND trfbcun <> '0' 
                        AND YEAR(TRFFECHA) = @Anio 
                        AND MONTH(TRFFECHA) = @Mes
                ) AS Tabla
                INNER JOIN 
                    DWCLIENTE 
                ON 
                    Cliente = COD_CLIENTE
                WHERE 
                    Cliente <> '0'";

            // Parámetros para la consulta
            SqlParameter[] parameters = {
                new SqlParameter("@Anio", anio),
                new SqlParameter("@Mes", mes)
            };

            try
            {
                // Ejecución de la consulta y procesamiento de resultados
                DataTable dt = _dbHelper.ExecuteSelectCommand(query, parameters);

                foreach (DataRow row in dt.Rows)
                {
                    listaClientes.Add(new DTO_IVETRF21Clientes
                    {
                        Cliente = int.Parse(row["Cliente"].ToString()),
                        Nombre = row["Nombre"].ToString(),
                        Tipo = int.Parse(row["Tipo"].ToString()),
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error en ConsultarIVETRF21ClientesPorFecha: " + ex.Message);
            }

            return listaClientes;
        }
        private List<DTO_DWCliente> ConsultarDWCliente(int codigoCliente)
        {
            List<DTO_DWCliente> resultado = new List<DTO_DWCliente>();
            string query = $"SELECT * FROM dwcliente WHERE cod_cliente = {codigoCliente}";

            try
            {
                DataTable dt = _dbHelper.ExecuteSelectCommand(query);
                foreach (DataRow row in dt.Rows)
                {
                    DTO_DWCliente cliente = new DTO_DWCliente
                    {
                        CodCliente = long.TryParse(row["cod_cliente"].ToString(), out var codCliente) ? codCliente : (long?)null,
                        CodClienteAnt = long.TryParse(row["cod_cliente_ant"].ToString(), out var codClienteAnt) ? codClienteAnt : (long?)null,
                        NombreCliente = row["nombrecliente"].ToString(),
                        Identificacion = row["identificacion"].ToString(),
                        TipoIdentificacion = int.TryParse(row["tipoidentificacion"].ToString(), out var tipoIdentificacion) ? tipoIdentificacion : (int?)null,
                        IdentUbicacion = short.TryParse(row["ident_ubicacion"].ToString(), out var identUbicacion) ? identUbicacion : (short?)null,
                        FNacimiento = int.TryParse(row["fnacimiento"].ToString(), out var fNacimiento) ? fNacimiento : (int?)null,
                        TipoCliente = int.TryParse(row["tipocliente"].ToString(), out var tipoCliente) ? tipoCliente : (int?)null,
                        OficialCuenta = short.TryParse(row["oficialcuenta"].ToString(), out var oficialCuenta) ? oficialCuenta : (short?)null,
                        Banca = int.TryParse(row["banca"].ToString(), out var banca) ? banca : (int?)null,
                        EstadoCivil = int.TryParse(row["estadocivil"].ToString(), out var estadoCivil) ? estadoCivil : (int?)null,
                        Genero = int.TryParse(row["genero"].ToString(), out var genero) ? genero : (int?)null,
                        Edad = int.TryParse(row["edad"].ToString(), out var edad) ? edad : (int?)null,
                        RangoEdad = int.TryParse(row["rangoedad"].ToString(), out var rangoEdad) ? rangoEdad : (int?)null,
                        ActividadEconomica = short.TryParse(row["actividadeconomica"].ToString(), out var actividadEconomica) ? actividadEconomica : (short?)null,
                        FechaAgregado = int.TryParse(row["fecha_agregado"].ToString(), out var fechaAgregado) ? fechaAgregado : (int?)null,
                        FechaModificado = int.TryParse(row["fecha_modificado"].ToString(), out var fechaModificado) ? fechaModificado : (int?)null,
                        GrupoEconomico = short.TryParse(row["grupoeconomico"].ToString(), out var grupoEconomico) ? grupoEconomico : (short?)null,
                        Profesion = short.TryParse(row["profesion"].ToString(), out var profesion) ? profesion : (short?)null,
                        Email = row["email"].ToString(),
                        Nit = row["nit"].ToString(),
                        PaisCliente = short.TryParse(row["pais_cliente"].ToString(), out var paisCliente) ? paisCliente : (short?)null,
                        Telefono1 = row["telefono1"].ToString(),
                        Telefono2 = row["telefono2"].ToString(),
                        Celular = row["celular"].ToString(),
                        Fax = row["fax"].ToString(),
                        Nombre1 = row["nombre1"].ToString(),
                        Nombre2 = row["nombre2"].ToString(),
                        Apellido1 = row["apellido1"].ToString(),
                        Apellido2 = row["apellido2"].ToString(),
                        ApellidoCasada = row["apellidocasada"].ToString(),
                        IngresoMensual = decimal.TryParse(row["ingresomensual"].ToString(), out var ingresoMensual) ? ingresoMensual : (decimal?)null,
                        RelacionDependencia = bool.TryParse(row["relacion_dependencia"].ToString(), out var relacionDependencia) && relacionDependencia,
                        LugarTrabajo = row["lugar_trabajo"].ToString(),
                        CargoTrabajo = row["cargo_trabajo"].ToString(),
                        ViviendaPropia = row["vivienda_propia"].ToString(),
                        Bloqueo = int.TryParse(row["bloqueo"].ToString(), out var bloqueo) ? bloqueo : (int?)null,
                        FultActualizacion = int.TryParse(row["fultactualizacion"].ToString(), out var fultActualizacion) ? fultActualizacion : (int?)null,
                        Cotitularidad = row["cotitularidad"].ToString(),
                        AgenciaApertura = short.TryParse(row["agenciaapertura"].ToString(), out var agenciaApertura) ? agenciaApertura : (short?)null,
                        CalificacionRiesgo = int.TryParse(row["calificacionriesgo"].ToString(), out var calificacionRiesgo) ? calificacionRiesgo : (int?)null,
                        CategoriaRiesgo = row["categoriariesgo"].ToString(),
                        NombreConyuge = row["nombre_conyuge"].ToString(),
                        NumHijos = int.TryParse(row["num_hijos"].ToString(), out var numHijos) ? numHijos : (int?)null,
                        EnFormacion = row["en_formacion"].ToString(),
                        IntermFinanciera = int.TryParse(row["interm_financiera"].ToString(), out var intermFinanciera) ? intermFinanciera : (int?)null,
                        NombreUsual = row["nombreusual"].ToString(),
                        FrecOperaciones = int.TryParse(row["frec_operaciones"].ToString(), out var frecOperaciones) ? frecOperaciones : (int?)null,
                        RefExternas = int.TryParse(row["ref_externas"].ToString(), out var refExternas) ? refExternas : (int?)null,
                        Fuente = int.TryParse(row["fuente"].ToString(), out var fuente) ? fuente : (int?)null,
                        Comentarios = row["comentarios"].ToString(),
                        FolioLibro = row["Folio_Libro"].ToString(),
                        FechaEscritura = int.TryParse(row["FechaEscritura"].ToString(), out var fechaEscritura) ? fechaEscritura : (int?)null,
                        Direccion = row["direccion"].ToString(),
                        DirPais = short.TryParse(row["dir_pais"].ToString(), out var dirPais) ? dirPais : (short?)null,
                        DirDepto = short.TryParse(row["dir_depto"].ToString(), out var dirDepto) ? dirDepto : (short?)null,
                        DirMunicpio = int.TryParse(row["dir_municpio"].ToString(), out var dirMunicpio) ? dirMunicpio : (int?)null,
                        Zona = int.TryParse(row["zona"].ToString(), out var zona) ? zona : (int?)null,
                        Colonia = row["colonia"].ToString(),
                        CodigoPostal = row["codigopostal"].ToString(),
                        RetImp = int.TryParse(row["ret_imp"].ToString(), out var retImp) ? retImp : (int?)null,
                        ConocimientoAct = int.TryParse(row["conocimiento_act"].ToString(), out var conocimientoAct) ? conocimientoAct : (int?)null,
                        Documentacion = int.TryParse(row["documentacion"].ToString(), out var documentacion) ? documentacion : (int?)null,
                        UbicacionNegocio = int.TryParse(row["ubicacionnegocio"].ToString(), out var ubicacionNegocio) ? ubicacionNegocio : (int?)null,
                        Categoria = int.TryParse(row["categoria"].ToString(), out var categoria) ? categoria : (int?)null,
                        Indicador = int.TryParse(row["indicador"].ToString(), out var indicador) ? indicador : (int?)null,
                        IdentSociedad = row["ident_sociedad"].ToString(),
                        IdentEmpresa = row["ident_empresa"].ToString(),
                        RangosQ = row["rangos_Q"].ToString(),
                        RangosD = row["rangos_D"].ToString(),
                        Email2 = row["email2"].ToString(),
                        Pep = row["Pep"].ToString(),
                        NombreParientePEP = row["NombreParientePEP"].ToString(),
                        Parentesco = row["Parentesco"].ToString(),
                        LugarTrabajoParientePEP = row["Lugar_TrabajoParientePEP"].ToString(),
                        CargoParientePEP = row["Cargo_ParientePEP"].ToString(),
                        EsFamiliarPEP = row["EsFamiliarPEP"].ToString(),
                        FormUltAviso = int.TryParse(row["form_ultaviso"].ToString(), out var formUltAviso) ? formUltAviso : (int?)null,
                        FormNumAvisos = short.TryParse(row["form_numavisos"].ToString(), out var formNumAvisos) ? formNumAvisos : (short?)null,
                        FormImpresion = int.TryParse(row["form_impresion"].ToString(), out var formImpresion) ? formImpresion : (int?)null,
                        FormAgImpresion = short.TryParse(row["form_agimpresion"].ToString(), out var formAgImpresion) ? formAgImpresion : (short?)null,
                        EstadoCl = int.TryParse(row["estadocl"].ToString(), out var estadoCl) ? estadoCl : (int?)null,
                        Sector = int.TryParse(row["Sector"].ToString(), out var sector) ? sector : (int?)null,
                        SubSector = int.TryParse(row["SubSector"].ToString(), out var subSector) ? subSector : (int?)null,
                        PosConsolidada = int.TryParse(row["PosConsolidada"].ToString(), out var posConsolidada) ? posConsolidada : (int?)null,
                        FichaSectorial = row["FichaSectorial"].ToString(),
                        GeneradorME = int.TryParse(row["GeneradorME"].ToString(), out var generadorME) ? generadorME : (int?)null,
                        DescActividadEco = row["descactividadeco"].ToString(),
                        Expediente = int.TryParse(row["expediente"].ToString(), out var expediente) ? expediente : (int?)null,
                        CatRiesgoBanguat = row["catriesgobanguat"].ToString(),
                        MRTipoPersona = short.TryParse(row["MRTipoPersona"].ToString(), out var mrTipoPersona) ? mrTipoPersona : (short?)null,
                        MRActividadEco = short.TryParse(row["MRActividadEco"].ToString(), out var mrActividadEco) ? mrActividadEco : (short?)null,
                        MRProfesion = short.TryParse(row["MRProfesion"].ToString(), out var mrProfesion) ? mrProfesion : (short?)null,
                        MRPaisOrigen = short.TryParse(row["MRPaisOrigen"].ToString(), out var mrPaisOrigen) ? mrPaisOrigen : (short?)null,
                        MRAgencia = short.TryParse(row["MRAgencia"].ToString(), out var mrAgencia) ? mrAgencia : (short?)null,
                        MRIngresos = short.TryParse(row["MRIngresos"].ToString(), out var mrIngresos) ? mrIngresos : (short?)null,
                        MRCategoria = short.TryParse(row["MRCategoria"].ToString(), out var mrCategoria) ? mrCategoria : (short?)null,
                        MRExpediente = short.TryParse(row["MRExpediente"].ToString(), out var mrExpediente) ? mrExpediente : (short?)null,
                        MRReferencias = short.TryParse(row["MRReferencias"].ToString(), out var mrReferencias) ? mrReferencias : (short?)null,
                        MRRangoIngresos = short.TryParse(row["MRRangoIngresos"].ToString(), out var mrRangoIngresos) ? mrRangoIngresos : (short?)null,
                        SubCategoria = short.TryParse(row["SubCategoria"].ToString(), out var subCategoria) ? subCategoria : (short?)null,
                        MRAntiguedad = short.TryParse(row["MRAntiguedad"].ToString(), out var mrAntiguedad) ? mrAntiguedad : (short?)null,
                        MRCantCtas = short.TryParse(row["MRCantCtas"].ToString(), out var mrCantCtas) ? mrCantCtas : (short?)null,
                        NegocioPropio = row["NegocioPropio"].ToString(),
                        PEPUltAct = int.TryParse(row["PEPUltAct"].ToString(), out var pepUltAct) ? pepUltAct : (int?)null,
                        PEPCond = row["PEPCond"].ToString(),
                        PEPPais = short.TryParse(row["PEPPais"].ToString(), out var pepPais) ? pepPais : (short?)null,
                        PEPFAgregado = int.TryParse(row["PEPFAgregado"].ToString(), out var pepFAgregado) ? pepFAgregado : (int?)null,
                        EsAsociadoPep = row["EsAsociadoPep"].ToString(),
                        NombreAsociadoPep = row["NombreAsociadoPep"].ToString(),
                        EmpMayor = row["EmpMayor"].ToString(),
                        EsCliente = int.TryParse(row["EsCliente"].ToString(), out var esCliente) ? esCliente : (int?)null,
                        MRIngresosMonto = decimal.TryParse(row["MRIngresosMonto"].ToString(), out var mrIngresosMonto) ? mrIngresosMonto : (decimal?)null,
                        GrupoAfinidadId = short.TryParse(row["GrupoAfinidadId"].ToString(), out var grupoAfinidadId) ? grupoAfinidadId : (short?)null,
                        FultMov = int.TryParse(row["fultmov"].ToString(), out var fultMov) ? fultMov : (int?)null,
                        MrActMasExpuesta = short.TryParse(row["MrActMasExpuesta"].ToString(), out var mrActMasExpuesta) ? mrActMasExpuesta : (short?)null,
                        MRProducto = short.TryParse(row["MRProducto"].ToString(), out var mrProducto) ? mrProducto : (short?)null,
                        MrComparacionIngresos = short.TryParse(row["MrComparacionIngresos"].ToString(), out var mrComparacionIngresos) ? mrComparacionIngresos : (short?)null,
                        MrCalificacion = int.TryParse(row["MrCalificacion"].ToString(), out var mrCalificacion) ? mrCalificacion : (int?)null,
                        VisitaFecha = int.TryParse(row["visita_fecha"].ToString(), out var visitaFecha) ? visitaFecha : (int?)null,
                        VisitaRespuesta = row["visita_respuesta"].ToString(),
                        VisitaComentario = row["visita_comentario"].ToString(),
                        RevisionFecha = int.TryParse(row["revision_fecha"].ToString(), out var revisionFecha) ? revisionFecha : (int?)null,
                        RevisionRespuesta = row["revision_respuesta"].ToString(),
                        RevisionComentario = row["revision_comentario"].ToString(),
                        UsrCalifica = row["usr_califica"].ToString(),
                        Actualizar = int.TryParse(row["Actualizar"].ToString(), out var actualizar) ? actualizar : (int?)null,
                        FAlertaAct = int.TryParse(row["fAlertaAct"].ToString(), out var fAlertaAct) ? fAlertaAct : (int?)null
                    };

                    resultado.Add(cliente);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(" Error al consultar ConsultarDWCliente " + ex.Message);
            }
            return resultado;
        }
        private List<DTO_IVE21TRF> ConsultarIVE21TRFPorFecha(int anio, int mes)
        {
            List<DTO_IVE21TRF> listaDatos = new List<DTO_IVE21TRF>();

            string queryReal = @"
                SELECT DISTINCT 
                    IVE21Transferencia.*, 
                    DWAGENCIA.*, 
                    dwcuenta_iban.iban AS ibano, 
                    dwcuenta_ibanben.iban AS ibanb
                FROM 
                    IVE21Transferencia
                LEFT OUTER JOIN 
                    DWAGENCIA ON AGENCIAID = TRFBRN
                LEFT OUTER JOIN 
                    [172.16.4.62].dwbinter.dbo.dwcuenta_iban dwcuenta_iban ON dwcuenta_iban.cuenta COLLATE SQL_Latin1_General_CP1_CI_AS = trfocta
                LEFT OUTER JOIN 
                    [172.16.4.62].dwbinter.dbo.dwcuenta_iban dwcuenta_ibanben ON dwcuenta_ibanben.cuenta COLLATE SQL_Latin1_General_CP1_CI_AS = trfbcta
                WHERE  
                    ive21 = 'S' 
                    AND YEAR(trffecha) = 2024  -- Reemplaza con el valor real de CmbAno.Text
                    AND MONTH(trffecha) = 1    -- Reemplaza con (CmbMes.ListIndex + 1)
                ORDER BY 
                    trffecha, trftipo, trftran;
            ";
            string query = @"
                SELECT DISTINCT 
                    IVE21Transferencia.*, 
                    DWAGENCIA.*, 
                    dwcuenta_iban.iban AS ibano, 
                    dwcuenta_ibanben.iban AS ibanb
                FROM 
                    IVE21Transferencia
                LEFT OUTER JOIN 
                    DWAGENCIA ON AGENCIAID = TRFBRN
                LEFT OUTER JOIN 
                    DBVariosFH_Remoto.dbo.dwcuenta_iban dwcuenta_iban 
                    ON CAST(dwcuenta_iban.cuenta AS NVARCHAR) COLLATE SQL_Latin1_General_CP1_CI_AS = CAST(trfocta AS NVARCHAR)
                LEFT OUTER JOIN 
                    DBVariosFH_Remoto.dbo.dwcuenta_iban dwcuenta_ibanben 
                    ON CAST(dwcuenta_ibanben.cuenta AS NVARCHAR) COLLATE SQL_Latin1_General_CP1_CI_AS = CAST(trfbcta AS NVARCHAR)
                WHERE  
                    ive21 = 'S' 
                    AND YEAR(trffecha) = @Anio  
                    AND MONTH(trffecha) = @Mes    
                ORDER BY 
                    trffecha, trftipo, trftran;";

            SqlParameter[] parameters = {
                new SqlParameter("@Anio", anio),
                new SqlParameter("@Mes", mes)
            };

            try
            {
                DataTable dt = _dbHelper.ExecuteSelectCommand(query, parameters);

                foreach (DataRow row in dt.Rows)
                {
                    listaDatos.Add(new DTO_IVE21TRF
                    {
                        // Propiedades de IVE21Transferencia
                        TRFFECHA = row["TRFFECHA"] as DateTime?,
                        TRFTIPO = row["TRFTIPO"]?.ToString(),
                        TRFTRAN = row["TRFTRAN"]?.ToString(),
                        TRFOCUN = row["TRFOCUN"]?.ToString(),
                        TRFOTPER = row["TRFOTPER"]?.ToString(),
                        TRFOTID = row["TRFOTID"]?.ToString(),
                        TRFOORD = row["TRFOORD"]?.ToString(),
                        TRFODOC = row["TRFODOC"]?.ToString(),
                        TRFOMUN = row["TRFOMUN"]?.ToString(),
                        TRFOAPE1 = row["TRFOAPE1"]?.ToString(),
                        TRFOAPE2 = row["TRFOAPE2"]?.ToString(),
                        TRFOAPEC = row["TRFOAPEC"]?.ToString(),
                        TRFONOM1 = row["TRFONOM1"]?.ToString(),
                        TRFONOM2 = row["TRFONOM2"]?.ToString(),
                        TRFOCTA = row["TRFOCTA"]?.ToString(),
                        TRFBCUN = row["TRFBCUN"]?.ToString(),
                        TRFBTPER = row["TRFBTPER"]?.ToString(),
                        TRFBTID = row["TRFBTID"]?.ToString(),
                        TRFBORD = row["TRFBORD"]?.ToString(),
                        TRFBDOC = row["TRFBDOC"]?.ToString(),
                        TRFBMUN = row["TRFBMUN"]?.ToString(),
                        TRFBAPE1 = row["TRFBAPE1"]?.ToString(),
                        TRFBAPE2 = row["TRFBAPE2"]?.ToString(),
                        TRFBAPEC = row["TRFBAPEC"]?.ToString(),
                        TRFBNOM1 = row["TRFBNOM1"]?.ToString(),
                        TRFBNOM2 = row["TRFBNOM2"]?.ToString(),
                        TRFBCTA = row["TRFBCTA"]?.ToString(),
                        TRFBBCO = row["TRFBBCO"] as int?,
                        TRFNUM = row["TRFNUM"]?.ToString(),
                        TRFPAIS = row["TRFPAIS"]?.ToString(),
                        TRFODEPT = row["TRFODEPT"]?.ToString(),
                        TRFDDEPT = row["TRFDDEPT"]?.ToString(),
                        TRFBRN = row["TRFBRN"]?.ToString(),
                        TRFMNT = row["TRFMNT"] as decimal?,
                        TRFCCY = row["TRFCCY"]?.ToString(),
                        TRFMNTD = row["TRFMNTD"] as decimal?,
                        Ive21 = row["ive21"]?.ToString(),
                        ID = row["ID"] as long? ?? 0,

                        // Propiedades de DWAGENCIA
                        AgenciaId = row["agenciaid"] as short?,
                        NombreAgencia = row["nombre"]?.ToString(),
                        DireccionAgencia = row["direccion"]?.ToString(),
                        DepartamentoAgencia = row["departamento"] as short?,
                        MunicipioAgencia = row["municipio"] as int?,
                        HorarioAgencia = row["horario"]?.ToString(),
                        AutoBanco = row["autobanco"]?.ToString(),
                        ATM = row["atm"]?.ToString(),
                        TipoAgencia = row["tipo"]?.ToString(),
                        RegionId = row["regionid"] as byte?,
                        Banca = row["banca"] as byte?,
                        Fapertura = row["fapertura"] as DateTime?,
                        Telefono = row["telefono"]?.ToString(),
                        Fax = row["fax"]?.ToString(),
                        CodigoPostal = row["codigopostal"]?.ToString(),
                        CodigoInterno = row["cod_interno"] as short?,
                        Traslado = row["traslado"] as short?,
                        UbicacionGeoId = row["ubicaciongeoid"] as short?,

                        // Propiedades de dwcuenta_iban
                        IbanIdOrigen = row["ibanid"] as int?,
                        CuentaOrigen = row["cuenta"] as long?,
                        IbanOrigen = row["ibano"]?.ToString(),
                        IbanFormatoOrigen = row["iban_formato"]?.ToString(),

                        // Propiedades de dwcuenta_ibanben
                        IbanIdBeneficiario = row["ibanid"] as int?,
                        CuentaBeneficiario = row["cuenta"] as long?,
                        IbanBeneficiario = row["ibanb"]?.ToString(),
                        IbanFormatoBeneficiario = row["iban_formato"]?.ToString(),
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error en ConsultarIVE21TRFPorFecha: " + ex.Message, ex);
            }

            return listaDatos;
        }


    }
}
