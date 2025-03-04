using IVEBA_API_Rest.Helpers;
using IVEBA_API_Rest.Models.IVE14EF;
using IVEBA_API_Rest.Models.IVE17DV;
using IVEBA_API_Rest.Models.IVEBA05;
using IVEBA_API_Rest.Models.IVECH;
using IVEBA_API_Rest.Services.IVEBA05;
using IVEBA_API_Rest.Utilidades;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Text;

namespace IVEBA_API_Rest.Services.IVE17DV
{
    public class IVEBA05HelperService : IIVEBA05HelperService
    {
        private readonly DbHelper _dbHelper;
        private readonly UtilidadesAPP utilidades;
        private List<DTO_DWCliente> clientesDW = new List<DTO_DWCliente>();
        public IVEBA05HelperService(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
            utilidades = new UtilidadesAPP();
        }

        public async Task<DTO_IVEBA05Response> GenerarArchivoIVEBA05(int mes, int ano)
        {
            int cantidadRegistrosOK = 0;
            DTO_IVEBA05Response response = new DTO_IVEBA05Response();
            string filePath = Path.Combine(Path.GetTempPath(), "archivoIVEBA05.txt");
            response.cantidadRegistrosOK = 0;
            response.cantidadRegistrosError = 0;
            response.archivoTXT = null;
            List<DTO_VistaIVEBA05Clientes> listaVistaIVEBA05Clientes = new List<DTO_VistaIVEBA05Clientes>();
            StringBuilder logErrores = new StringBuilder();

            try
            {                
                List<DTO_IVEBA05Archivos> registros = ConsultarArchivosPorMesAno(mes, ano);                                

                // Devuelve archivo existente   
                if (registros.Count > 0)
                {
                    using (StreamWriter archivo = new StreamWriter(filePath))
                    {
                        foreach (var registro in registros)
                        {                                                        
                            string stringGrabar = $"{registro.StringValue}";
                            stringGrabar = utilidades.QuitoTildes(stringGrabar);
                            archivo.WriteLine(stringGrabar);
                            cantidadRegistrosOK++;

                        }
                    }
                    byte[] fileBytesExistente = File.ReadAllBytes(filePath);
                    File.Delete(filePath);
                    response.cantidadRegistrosOK = cantidadRegistrosOK;
                    response.cantidadRegistrosError = 0;
                    response.archivoTXT = fileBytesExistente;
                    return response;
                }
                else
                {
                    // Genera archivo nuevo      
                    DeleteIVEBA05Temporal();
                    listaVistaIVEBA05Clientes = ConsultarClientesVistaIVEBA05(mes, ano);
                    string codigosClientes = string.Join(",", listaVistaIVEBA05Clientes.Select(cliente => cliente.Cliente.ToString()));

                    //Consultas por unica vez en cada proceso para no recargar la BD
                    clientesDW = ConsultarDWClienteTodos(codigosClientes);

                    string datosPersona = "Prueba";

                    foreach (var cliente in listaVistaIVEBA05Clientes)
                    {
                        switch (cliente.Tipo)
                        {
                            case 2:
                            case 3:                                
                                if (ProcesosFisicos(cliente.Cliente, out datosPersona))
                                {
                                    InsertaIVEBA05Temporal(cliente.Cliente, datosPersona, cliente.Dia, mes, ano);
                                    logErrores.AppendLine($"{cliente.Cliente.ToString("D12")} {cliente.NombreCliente} Error al procesar físico");
                                    response.cantidadRegistrosOK++;
                                }
                                else
                                {
                                    InsertaIVEBA05Temporal(cliente.Cliente, "ERROR", 99, 99, 9999);
                                    response.cantidadRegistrosError++;
                                }
                             break;

                            case 4:                                
                                if (ProcesosJuridicos(cliente.Cliente, out datosPersona))
                                {
                                    InsertaIVEBA05Temporal(cliente.Cliente, datosPersona, cliente.Dia, mes, ano);
                                    response.cantidadRegistrosOK++;
                                }
                                else
                                {
                                    InsertaIVEBA05Temporal(cliente.Cliente, "ERROR", 99, 99, 9999);
                                    logErrores.AppendLine($"{cliente.Cliente.ToString("D12")} {cliente.NombreCliente} Error al procesar jurídico");
                                    response.cantidadRegistrosError++;
                                }
                                break;
                            default:
                                response.cantidadRegistrosError++;
                                break; 

                        }
                    }
                }
                // *****************************
                // ARCHIVO CON ERRORES 
                // *****************************
                await File.WriteAllTextAsync(filePath, logErrores.ToString());
                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                File.Delete(filePath);
                response.archivoTXT = fileBytes;

                // *****************************
                // ARCHIVO OK
                // *****************************
                response.archivoTXT = TransaccionesClientes(true, filePath, mes, ano);              
            }
            catch (Exception ex)
            {
                throw new Exception("Error en GenerarArchivoIVEBA05: " + ex.Message);
            }

            return response;
        }

        private bool ProcesosFisicos(int cliente, out string stringDatos)
        {
            try
            {
                stringDatos = string.Empty;
                var stringArmado = "";

                // Consultar la base de datos para obtener el cliente
                // Buscar cliente en la lista
                List<DTO_DWCliente> clientes = clientesDW.Where(x => x.CodCliente == cliente).ToList();

                if (clientes == null || clientes.Count == 0)
                    return false;

                var varFisico = clientes.First();

                stringArmado = "I";
                string orden = string.Empty;

                switch (varFisico.TipoIdentificacion)
                {
                    case 1: // Cédula
                        orden = varFisico.Identificacion.Substring(0, 3);
                        orden = orden.Replace("#", "Ñ");

                        if ((char.ToUpper(orden[0]) < 'A' || char.ToUpper(orden[0]) > 'Z') && orden[0] != 'Ñ')
                            throw new Exception($"Orden de Cédula incorrecta {varFisico.Identificacion}");

                        if (orden[1] == '0')
                            orden = orden[0].ToString() + orden[2];

                        orden = FormateoString(orden, 3, ' ', true);
                        stringArmado += "C" + orden;
                        stringArmado += FormateoString(varFisico.Identificacion.Substring(4, 7), 15, ' ', true);
                        break;

                    case 2: // Partida
                        stringArmado += "N   ";
                        stringArmado += FormateoString(varFisico.Identificacion.Trim(), 15, ' ', true);
                        break;

                    case 4:
                    case 22: // Pasaporte
                        stringArmado += "P   ";
                        stringArmado += FormateoString(varFisico.Identificacion.Trim(), 15, ' ', true);
                        break;

                    case 26: // DPI
                        stringArmado += "D   ";
                        stringArmado += FormateoString(varFisico.Identificacion.Trim(), 15, ' ', true);
                        break;

                    default:
                        stringArmado += "O   ";
                        stringArmado += FormateoString(varFisico.Identificacion.Trim(), 15, ' ', true);
                        break;
                }

                stringArmado += FormateoString(QuitoTildes(varFisico.Apellido1?.ToUpper() ?? ""), 15, ' ', true);
                stringArmado += FormateoString(QuitoTildes(varFisico.Apellido2?.ToUpper() ?? ""), 15, ' ', true);
                stringArmado += FormateoString(QuitoTildes(varFisico.ApellidoCasada?.ToUpper() ?? ""), 15, ' ', true);
                stringArmado += FormateoString(QuitoTildes(varFisico.Nombre1?.ToUpper() ?? ""), 15, ' ', true);
                stringArmado += FormateoString(QuitoTildes(varFisico.Nombre2?.ToUpper() ?? ""), 15, ' ', true);

                string fechaNacimiento = varFisico.FNacimiento != null
                ? Convert.ToDateTime(varFisico.FNacimiento).ToString("yyyyMMdd")
                : "00000000"; // Valor por defecto si la fecha es nula
                stringArmado += fechaNacimiento + "GT";
                stringDatos = stringArmado;
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error en ProcesoFisicos: " + ex.Message, ex);
            }
        }

        private bool ProcesosJuridicos(int cliente, out string stringDatos)
        {
            try
            {
                stringDatos = string.Empty;
                var stringArmado = "";

                // Buscar cliente en la lista
                List<DTO_DWCliente> clientes = clientesDW.Where(x => x.CodCliente == cliente).ToList();

                if (clientes == null || clientes.Count == 0)
                    return false;

                var varEmpresa = clientes.First();

                // Determinar el NIT de la empresa
                string nitEmpresa = string.IsNullOrWhiteSpace(varEmpresa.Nit) ? "" : varEmpresa.Nit.Trim();

                if (string.IsNullOrEmpty(nitEmpresa))
                {
                    if (varEmpresa.TipoIdentificacion == 8)
                    {
                        nitEmpresa = varEmpresa.Identificacion?.Trim() ?? "";
                    }
                    else
                    {
                        nitEmpresa = "SINNIT";
                        // Incrementar contador de NIT si es necesario
                        // txtNit.Text = (int.Parse(txtNit.Text) + 1).ToString();
                    }
                }

                // Construir la cadena de salida
                stringArmado = "J";
                stringArmado += "N";
                stringArmado += "   ";
                stringArmado += FormateoString(QuitoCaracter(nitEmpresa), 15, ' ', true);
                stringArmado += FormateoString(QuitoTildes(varEmpresa.NombreCliente?.Trim() ?? ""), 75, ' ', true);

                //// Formatear la fecha de nacimiento               
                //string fechaNacimiento = "00000000";
                //if (varEmpresa.FNacimiento != null)
                //{
                //    string fnacimientoStr = varEmpresa.FNacimiento.ToString();

                //    if (fnacimientoStr.Length == 8 && int.TryParse(fnacimientoStr, out _))
                //    {
                //        fechaNacimiento = DateTime.ParseExact(fnacimientoStr, "yyyyMMdd", CultureInfo.InvariantCulture)
                //                                  .ToString("yyyyMMdd");
                //    }
                //}

                stringArmado += varEmpresa.FNacimiento;
                stringArmado += "GT";

                stringDatos = stringArmado;
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error en ProcesoJuridicos: " + ex.Message, ex);
            }
        }


        public byte[] TransaccionesClientes(bool tipoArchivo, string filePath, int mes, int año)
        {
            try
            {
                // Obtener datos desde la base de datos
                List<DTO_IVE_BA05_Impresion> registros = ConsultarDatosImpresionIVEBA05(mes, año);
                if (registros == null || registros.Count == 0)
                    return null;

                // Obtener la lista de agencias para mejorar el rendimiento
                List<DTO_IVE_AGENCIAS> agencias = ConsultarIVEAgencias();

                string fechaStr = DateTime.Now.ToString("yyyyMMddHHmmss");
                int ordinal = 0;
                string nombreCliente = "";

                using (StreamWriter fileWriter = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    foreach (var registro in registros)
                    {
                        ordinal++;
                        string stringGrabar = registro.Fecha.ToString("yyyyMMdd"); // Formateo de fecha
                        stringGrabar += registro.Datos.Trim();

                        // Procesamiento de cuenta de origen
                        string noCuenta = registro.Origen.Contains("-")
                            ? QuitoCaracter(registro.Origen.Trim())
                            : registro.Origen.Trim();

                        if (!string.IsNullOrEmpty(registro.Iban))
                        {
                            stringGrabar += FormateoString(registro.Iban, 28, ' ', true);
                        }
                        else
                        {
                            if (long.TryParse(noCuenta, out _))
                            {
                                stringGrabar += FormateoString(noCuenta, 28, ' ', true);
                            }
                            else
                            {
                                if (registro.Transaccion == 201)
                                {
                                    stringGrabar += FormateoString(noCuenta.Substring(0, 10), 28, ' ', true);
                                }
                                else
                                {
                                    noCuenta = registro.Fecha.ToString("yyyyMMdd") +
                                               (registro.Sucursal - 500).ToString("00") +
                                               registro.Asiento.ToString("0000000");

                                    stringGrabar += FormateoString(noCuenta, 28, ' ', true);
                                }
                            }
                        }

                        stringGrabar += registro.Transaccion.ToString();

                        // Moneda y montos
                        if (registro.Moneda == 0)
                        {
                            stringGrabar += "GTQ";
                            stringGrabar += FormateoString(FormateoMontos(registro.MontoQ.ToString("#0.00")), 14, ' ', true);
                            stringGrabar += FormateoString(FormateoMontos(registro.MontoD.ToString("#0.00")), 14, ' ', true);
                        }
                        else if (registro.Moneda == 2222)
                        {
                            stringGrabar += "USD";
                            stringGrabar += FormateoString(FormateoMontos(registro.MontoD.ToString("#0.00")), 14, ' ', true);
                            stringGrabar += FormateoString(FormateoMontos(registro.MontoD.ToString("#0.00")), 14, ' ', true);
                        }

                        // Obtener departamento con la función ConsultarIVEAgencias
                        var agencia = agencias.FirstOrDefault(a => a.C6021 == registro.Sucursal);
                        string departamento = agencia != null ? agencia.C6025.ToString("00") : "01";
                        stringGrabar += departamento;

                        // Control de clientes en el archivo preliminar
                        if (!tipoArchivo)
                        {
                            if (string.IsNullOrEmpty(nombreCliente))
                            {
                                nombreCliente = stringGrabar.Substring(28, 75);
                            }
                            else if (!string.Equals(nombreCliente.Trim(), stringGrabar.Substring(28, 75).Trim()))
                            {
                                nombreCliente = stringGrabar.Substring(28, 75);
                            }
                            else
                            {
                                stringGrabar = stringGrabar.Remove(28, 75).Insert(28, new string(' ', 75));
                            }
                        }

                        // Escribir en archivo
                        fileWriter.WriteLine(stringGrabar);

                        // Si es definitivo, guardar en base de datos
                        if (tipoArchivo)
                        {
                            InsertaIVEBA05Archivo(fechaStr, filePath, ordinal, mes, año, stringGrabar);
                        }
                    }
                }

                // Leer el archivo generado y devolverlo como byte[]
                byte[] fileBytes = File.ReadAllBytes(filePath);

                // Opcional: Eliminar el archivo después de la lectura
                File.Delete(filePath);

                return fileBytes;
            }
            catch (Exception ex)
            {
                throw new Exception("Error en TransaccionesClientes: " + ex.Message, ex);
            }
        }


        private void InsertaIVEBA05Archivo(string fechaStr, string archivo, int ordinal, int mes, int ano, string stringGrabar)
        {
            string insertQuery = @"
                INSERT INTO IVE_BA_05_Archivos 
                (Fecha, Archivo, Ordinal, Mes, Ano, String) 
                VALUES (@FechaStr, @Archivo, @Ordinal, @Mes, @Ano, @StringGrabar)";

            try
            {
                SqlParameter[] parameters = {
            new SqlParameter("@FechaStr", fechaStr),
            new SqlParameter("@Archivo", archivo),
            new SqlParameter("@Ordinal", ordinal),
            new SqlParameter("@Mes", mes),
            new SqlParameter("@Ano", ano),
            new SqlParameter("@StringGrabar", stringGrabar.Trim())
        };

                _dbHelper.ExecuteNonQuery(insertQuery, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception("Error en InsertaIVEBA05Archivo: " + ex.Message);
            }
        }


        public List<DTO_IVE_AGENCIAS> ConsultarIVEAgencias()
        {
            List<DTO_IVE_AGENCIAS> listaDatos = new List<DTO_IVE_AGENCIAS>();

            string query = "SELECT * FROM IVE_AGENCIAS";

            try
            {
                DataTable dt = _dbHelper.ExecuteSelectCommand(query);

                foreach (DataRow row in dt.Rows)
                {
                    listaDatos.Add(new DTO_IVE_AGENCIAS
                    {
                        TZ_LOCK = Convert.ToDecimal(row["TZ_LOCK"]),
                        C6020 = row["C6020"].ToString(),
                        C6021 = Convert.ToInt16(row["C6021"]),
                        C6022 = row["C6022"].ToString(),
                        C6023 = Convert.ToInt32(row["C6023"]),
                        C6024 = row["C6024"].ToString(),
                        C6025 = Convert.ToInt16(row["C6025"]),
                        C6026 = Convert.ToInt32(row["C6026"]),
                        C6027 = Convert.ToDouble(row["C6027"]),
                        C6028 = Convert.ToDouble(row["C6028"]),
                        C6029 = Convert.ToDouble(row["C6029"]),
                        C6030 = row["C6030"].ToString(),
                        C6031 = Convert.ToDouble(row["C6031"]),
                        C6032 = row["C6032"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error en ConsultarIVEAgencias: " + ex.Message);
            }

            return listaDatos;
        }


        public List<DTO_IVE_BA05_Impresion> ConsultarDatosImpresionIVEBA05(int mes, int año)
        {
            List<DTO_IVE_BA05_Impresion> listaDatos = new List<DTO_IVE_BA05_Impresion>();

            string query = @"
                SELECT 
                    VISTA_IVE_BA_05_Impresion.*, 
                    iban 
                FROM 
                    VISTA_IVE_BA_05_Impresion 
                LEFT OUTER JOIN 
                    DBVariosFH_Remoto.dbo.dwcuenta_iban 
                ON 
                    dwcuenta_iban.cuenta = VISTA_IVE_BA_05_Impresion.origen 
                WHERE 
                    MONTH(Fecha) = @Mes 
                    AND YEAR(Fecha) = @Año
                ORDER BY 
                    Fecha";

                    SqlParameter[] parameters = {
                new SqlParameter("@Mes", mes),
                new SqlParameter("@Año", año)
            };

            try
            {
                DataTable dt = _dbHelper.ExecuteSelectCommand(query, parameters);

                foreach (DataRow row in dt.Rows)
                {
                    listaDatos.Add(new DTO_IVE_BA05_Impresion
                    {
                        Sucursal = Convert.ToInt16(row["Sucursal"]),
                        Numero = Convert.ToInt64(row["Numero"]),
                        Fecha = Convert.ToDateTime(row["Fecha"]),
                        Asiento = Convert.ToInt64(row["Asiento"]),
                        Cliente = Convert.ToDouble(row["Cliente"]),
                        Estado = row["Estado"].ToString(),
                        Moneda = Convert.ToInt16(row["Moneda"]),
                        MontoQ = Convert.ToDouble(row["MontoQ"]),
                        MontoD = Convert.ToDouble(row["Monto$"]), // Mapeado correctamente
                        Transaccion = Convert.ToInt16(row["Transaccion"]),
                        Origen = row["Origen"].ToString(),
                        Destino = row["Destino"].ToString(),
                        Informacion = row["Informacion"].ToString(),
                        Opcion = Convert.ToInt16(row["Opcion"]),
                        Datos = row["Datos"].ToString(),
                        Iban = row["iban"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error en ConsultarDatosIVEBA05: " + ex.Message);
            }

            return listaDatos;
        }

        private List<DTO_DWCliente> ConsultarDWClienteTodos(string clientes)
        {
            List<DTO_DWCliente> resultado = new List<DTO_DWCliente>();
            string query = $"SELECT * FROM dwcliente WHERE cod_cliente in ({clientes})";

            try
            {
                DataTable dt = _dbHelper.ExecuteSelectCommand(query);
                foreach (DataRow row in dt.Rows)
                {
                    DTO_DWCliente cliente = new DTO_DWCliente
                    {
                        CodCliente = int.TryParse(row["cod_cliente"].ToString(), out var codCliente) ? codCliente : (int?)null,
                        CodClienteAnt = int.TryParse(row["cod_cliente_ant"].ToString(), out var cod_cliente_ant) ? cod_cliente_ant : (int?)null,
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
        public List<DTO_IVEBA05Archivos> ConsultarArchivosPorMesAno(int mes, int ano)
        {
            List<DTO_IVEBA05Archivos> listaDatos = new List<DTO_IVEBA05Archivos>();

            string query = @"SELECT * FROM IVE_BA_05_Archivos 
                             WHERE Mes = @Mes AND Ano = @Ano";

            SqlParameter[] parameters = {
                new SqlParameter("@Mes", mes),
                new SqlParameter("@Ano", ano)
            };

            try
            {
                DataTable dt = _dbHelper.ExecuteSelectCommand(query, parameters);

                foreach (DataRow row in dt.Rows)
                {
                    listaDatos.Add(new DTO_IVEBA05Archivos
                    {
                        Fecha = row["Fecha"].ToString(),
                        Archivo = row["Archivo"].ToString(),
                        Ordinal = Convert.ToInt32(row["Ordinal"]),
                        Mes = Convert.ToInt32(row["Mes"]),
                        Ano = Convert.ToInt32(row["Ano"]),
                        StringValue = row["String"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error en ConsultarArchivosPorMesAno: " + ex.Message);
            }

            return listaDatos;
        }

        public List<DTO_VistaIVEBA05Clientes> ConsultarClientesVistaIVEBA05(int mes, int ano)
        {
            List<DTO_VistaIVEBA05Clientes> listaDatos = new List<DTO_VistaIVEBA05Clientes>();

            string query = @"SELECT DISTINCT Cliente, NombreCliente as Nombre, TipoCliente as Tipo, Dia, Mes, Ano
                     FROM Vista_IVE_BA_05
                     LEFT OUTER JOIN DBVariosFH_Remoto.dbo.dwcliente ON Cliente = Cod_Cliente
                     WHERE Cliente <> 6 AND Total >= 10000 AND Mes = @Mes AND Ano = @Ano";

            SqlParameter[] parameters = {
                new SqlParameter("@Mes", mes),
                new SqlParameter("@Ano", ano)
            };

            try
            {
                DataTable dt = _dbHelper.ExecuteSelectCommand(query, parameters);

                foreach (DataRow row in dt.Rows)
                {
                    listaDatos.Add(new DTO_VistaIVEBA05Clientes
                    {
                        Cliente = int.Parse(row["Cliente"].ToString()),
                        NombreCliente = row["Nombre"].ToString(),
                        Tipo = int.Parse(row["Tipo"].ToString()),
                        Dia = int.Parse(row["Dia"].ToString()),
                        Mes = int.Parse(row["Mes"].ToString()),
                        Ano = int.Parse(row["Ano"].ToString())
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error en ConsultarClientesVistaIVEBA05: " + ex.Message);
            }

            return listaDatos;
        }

        private void DeleteIVEBA05Temporal()
        {
            string query = "Delete from IVE_BA_05_Temporal";
            try
            {
                _dbHelper.ExecuteNonQuery(query);
            }
            catch (Exception ex)
            {
                throw new Exception(" Error al eliminar DeleteIVEBA05Temporal " + ex.Message);
            }
        }

        private void InsertaIVEBA05Temporal(int cliente, string str, int dia, int mes, int ano)
        {
            string deleteQuery = "DELETE FROM IVE_BA_05_Temporal";
            string insertQuery = @"INSERT INTO IVE_BA_05_Temporal (Cliente, String, Dia, Mes, Ano) 
                         VALUES (@Cliente, @String, @Dia, @Mes, @Ano)";

            try
            {
                _dbHelper.ExecuteNonQuery(deleteQuery);

                SqlParameter[] parameters = {
                    new SqlParameter("@Cliente", cliente),
                    new SqlParameter("@String", str.Trim()),
                    new SqlParameter("@Dia", dia),
                    new SqlParameter("@Mes", mes),
                    new SqlParameter("@Ano", ano)
                };

                _dbHelper.ExecuteNonQuery(insertQuery, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception("Error en InsertaIVEBA05Temporal: " + ex.Message);
            }
        }

        // Función para formatear cadenas
        private string FormateoString(string stringAFormatear, int digitos, char relleno, bool orientacionDerecha = false)
        {
            string stringFormateado = stringAFormatear.Trim();
            int longitud = stringFormateado.Length;

            if (longitud <= digitos)
            {
                if (!orientacionDerecha)
                {
                    return stringFormateado.PadLeft(digitos, relleno);
                }
                else
                {
                    return stringFormateado.PadRight(digitos, relleno);
                }
            }
            else
            {
                return stringFormateado.Substring(0, digitos);
            }
        }

        // Función para formatear montos
        private string FormateoMontos(string montoATransformar)
        {
            if (decimal.TryParse(montoATransformar, out decimal monto))
            {
                monto *= 100;
                return ((int)monto).ToString();
            }
            else
            {
                return "0"; // Valor por defecto si la conversión falla
            }
        }


        public string QuitoTildes(string stringQuitar)
        {
            return stringQuitar
                .Replace("Á", "A")
                .Replace("É", "E")
                .Replace("Í", "I")
                .Replace("Ó", "O")
                .Replace("Ú", "U")
                .Replace("-", " ")
                .Replace("/", " ")
                .Replace("$", " ");
        }
        public string QuitoCaracter(string stringQuitar)
        {
            string stringTemporal = stringQuitar.Replace(",", "").Replace("-", "");
            return stringTemporal;
        }
    }
}
