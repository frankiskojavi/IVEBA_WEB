using IVEBA_API_Rest.Helpers;
using IVEBA_API_Rest.Models.IVE21TRF;
using IVEBA_API_Rest.Models.IVECH;
using IVEBA_API_Rest.Utilidades;
using Microsoft.Win32;
using System.Data;
using System.Data.SqlClient;
using System.IO.Pipelines;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IVEBA_API_Rest.Services.IVECH
{
    public class IVECHHelperService : IIVECHHelperService
    {
        private readonly DbHelper _dbHelper;
        private readonly IConfiguration _configuration;
        private readonly UtilidadesAPP utilidades;

        private int contadorNit = 0;
        private int cantidadRegsDetalleOK = 0;
        private int cantidadRegsDetalleERROR = 0;

        public IVECHHelperService(DbHelper dbHelper, IConfiguration configuration)
        {
            _dbHelper = dbHelper;
            _configuration = configuration;
            utilidades = new UtilidadesAPP();
        }

        public async Task<DTO_CHCajaTemporalResponse> GenerarArchivoCH(bool archivoDefinitivo, int fechaInicial, int fechaFinal){
            DTO_CHCajaTemporalResponse response = new DTO_CHCajaTemporalResponse();
            List<DTO_IVECHClientesCaja> clientesCaja = new List<DTO_IVECHClientesCaja>();
            string filePath = Path.Combine(Path.GetTempPath(), "archivoGenerado.txt");
            string datosPersona = "";
            string datosEmpresa = "";
            int cantidadRegistrosOK = 0;
            StringBuilder logErrores = new StringBuilder();

            try
            {
                List<DTO_IVECHCajaArchivos> registros = ConsultarIVECHCajaPorRangoFechas(fechaInicial, fechaFinal);                

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

                    EliminaCHCajaTemporal();
                    clientesCaja = ConsultarClientesCHCajaTemporal(fechaInicial, fechaFinal);
                    foreach (DTO_IVECHClientesCaja clienteCaja in clientesCaja)
                    {
                        clienteCaja.Nombre = clienteCaja.Nombre.Replace("'", "");
                        if (clienteCaja.Cliente != 0)
                        {
                            switch (clienteCaja.Tipo)
                            {
                                case 2:
                                case 3:
                                    if (ProcesoFisicos(clienteCaja.Cliente, out datosPersona))
                                    {
                                        InsertarCHCajaTemporal(clienteCaja.Cliente, datosPersona, 0, 0, 0);
                                        response.registrosOKEncabezado++;
                                    }
                                    else
                                    {
                                        InsertarCHCajaTemporal(clienteCaja.Cliente, "ERROR", 99, 99, 9999);
                                        logErrores.AppendLine($"{clienteCaja.Cliente.ToString("D12")} {clienteCaja.Nombre} Error al procesar físico");
                                        response.registrosOKEncabezado++;
                                    }
                                    break;
                                case 4:
                                case 1:
                                    if (ProcesoJuridicos(clienteCaja.Cliente, out datosEmpresa))
                                    {
                                        datosEmpresa = datosEmpresa.Replace("'", " ");
                                        InsertarCHCajaTemporal(clienteCaja.Cliente, datosEmpresa, 0, 0, 0);
                                        response.registrosOKEncabezado++;
                                    }
                                    else
                                    {
                                        InsertarCHCajaTemporal(clienteCaja.Cliente, "ERROR", 99, 99, 9999);
                                        logErrores.AppendLine($"{clienteCaja.Cliente.ToString("D12")} {clienteCaja.Nombre} Error al procesar jurídico");
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
                throw new Exception(" Error en GenerarArchivoCH " + ex.Message);
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
                        stringArmado += "P" + orden +  utilidades.FormateoString2(" ", 3, ' ', true) + utilidades.FormateoString2(clienteData.Identificacion, 20, ' ', true);
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
            }catch(Exception ex)
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
            }catch(Exception ex)
            {
                throw new Exception(" Error en ProcesoJuridicos " + ex.Message);
            }
        }

        private byte[] TransaccionesClientes(bool tipoArchivo, string filePath, int fechaInicio, int fechaFin)
        {
            try
            {
                List<DTO_VCHCaja> registros = ConsultarVCHCaja(fechaInicio, fechaFin);

                using (StreamWriter fileWriter = new StreamWriter(filePath, append: false))
                {
                    int ordinal = 0;
                    bool grabar = true;

                    foreach (DTO_VCHCaja registro in registros)
                    {
                        ordinal++;
                        //string stringGrabar = registro.FEC.ToString("yyyyMMdd"); // Formateo de fecha
                        string stringGrabar = registro.FEC.ToString(); // Formateo de fecha
                        stringGrabar += utilidades.FormateoString2(registro.Cheq.Trim(), 15, ' ', true);

                        //-- Se inicia registrando los beneficiarios de los cheques del banco
                        //-- Proceso los registros del banco (cheques de caja emitidos por el banco)
                        if (registro.TPS == "B")
                        {
                            //-- Si el beneficiario no es cliente del banco, se toma la información que se solicitó de los beneficiarios
                            if (registro.CLT.Trim() == "0")
                            {
                                stringGrabar += utilidades.FormateoString2(registro.TPB.Trim(), 1, ' ', true);

                                //-- Si es persona individual, se toma el detalle de cada nombre
                                if (registro.TPB.Trim() == "I")
                                {
                                    stringGrabar += utilidades.FormateoString2(registro.PAB.ToUpper().Trim(), 15, ' ', true);
                                    stringGrabar += utilidades.FormateoString2(registro.SAB.ToUpper().Trim(), 15, ' ', true);
                                    stringGrabar += string.IsNullOrEmpty(registro.ACB) ? new string(' ', 15) : utilidades.FormateoString2(registro.ACB.ToUpper().Trim(), 15, ' ', true);
                                    stringGrabar += utilidades.FormateoString2(registro.PNB.ToUpper().Trim(), 15, ' ', true);
                                    stringGrabar += utilidades.FormateoString2(registro.SNB.ToUpper().Trim(), 15, ' ', true);
                                }
                                else
                                {
                                    //-- Si es jurídica, se toma el nombre de la empresa
                                    stringGrabar += utilidades.FormateoString2(registro.NJB.ToUpper().Trim(), 75, ' ', true);
                                }
                            }
                            else
                            {
                                //-- Si es cliente, se busca información en la tabla temporal que se leyó anteriormente y se registra el string de `clt`
                                var dataTemporal = ConsultarIVECHCajaTemporalPorCliente(registro.CLT.Trim());
                                if (dataTemporal != null && dataTemporal.String.ToUpper() != "ERROR")
                                {
                                    stringGrabar += dataTemporal.String.Substring(0, 1) + utilidades.FormateoString2(dataTemporal.String.Substring(25), 75, ' ', true);
                                    grabar = true;
                                }
                                else
                                {
                                    stringGrabar += utilidades.FormateoString2("", 76, ' ', true);
                                    grabar = false;
                                }
                            }

                            //-- Le agrego el registro solicitante como banco
                            stringGrabar += "JN" + new string(' ', 3) + "1205544" + new string(' ', 13) + "BANCO INTERNACIONAL, S.A." + new string(' ', 50);
                        }
                        else
                        {
                            //-- Procesan los cheques de caja vendidos
                            //-- Si el beneficiario no es cliente del banco, se toma la información de la BD
                            stringGrabar += utilidades.FormateoString2(registro.TPB.Trim(), 1, ' ', true);

                            //-- Si es persona individual, se toma el detalle de cada nombre
                            if (registro.TPB.Trim() == "I")
                            {
                                stringGrabar += utilidades.FormateoString2(registro.PAB.ToUpper().Trim(), 15, ' ', true);
                                stringGrabar += utilidades.FormateoString2(registro.SAB.ToUpper().Trim(), 15, ' ', true);
                                stringGrabar += string.IsNullOrEmpty(registro.ACB) ? new string(' ', 15) : utilidades.FormateoString2(registro.ACB.ToUpper().Trim(), 15, ' ', true);
                                stringGrabar += utilidades.FormateoString2(registro.PNB.ToUpper().Trim(), 15, ' ', true);
                                stringGrabar += utilidades.FormateoString2(registro.SNB.ToUpper().Trim(), 15, ' ', true);
                            }
                            else
                            {
                                //-- Si es jurídica, se toma el nombre de la empresa
                                stringGrabar += utilidades.FormateoString2(registro.NJB.ToUpper().Trim(), 75, ' ', true);
                            }

                            //-- Si el solicitante no es cliente del banco, se toma la información de la BD
                            if (registro.CLT.Trim() == "0")
                            {
                                stringGrabar += utilidades.FormateoString2(registro.TPS.Trim(), 1, ' ', true);

                                //-- Si es persona individual, se toma el detalle de cada nombre
                                if (registro.TPS.Trim() == "I")
                                {
                                    if (registro.IPS.Trim() == "C")
                                    {
                                        stringGrabar += utilidades.FormateoString2(registro.IPS.Trim(), 1, ' ', true);
                                        stringGrabar += utilidades.FormateoString2(registro.NOS.Trim(), 3, ' ', true);

                                        string cedula = registro.NIS.Trim();
                                        int posicion = cedula.IndexOf(' ');
                                        if (posicion != -1)
                                        {
                                            if (cedula.IndexOf(' ', posicion + 1) != -1)
                                            {
                                                cedula = cedula.Substring(cedula.IndexOf(' ', posicion + 1) + 1).Replace(" ", "");
                                            }
                                            else
                                            {
                                                cedula = cedula.Substring(posicion + 1).Replace(" ", "");
                                            }
                                        }
                                        stringGrabar += utilidades.FormateoString2(cedula.ToUpper(), 20, ' ', true);
                                    }
                                    else
                                    {
                                        stringGrabar += utilidades.FormateoString2(registro.IPS.Trim(), 1, ' ', true);
                                        stringGrabar += new string(' ', 3);
                                        stringGrabar += utilidades.FormateoString2(registro.NIS.ToUpper().Trim(), 20, ' ', true);
                                    }

                                    stringGrabar += utilidades.FormateoString2(registro.PAS.ToUpper().Trim(), 15, ' ', true);
                                    stringGrabar += utilidades.FormateoString2(registro.SAS.ToUpper().Trim(), 15, ' ', true);
                                    stringGrabar += string.IsNullOrEmpty(registro.ACS) ? new string(' ', 15) : utilidades.FormateoString2(registro.ACS.ToUpper().Trim(), 15, ' ', true);
                                    stringGrabar += utilidades.FormateoString2(registro.PNS.ToUpper().Trim(), 15, ' ', true);
                                    stringGrabar += utilidades.FormateoString2(registro.SNS.ToUpper().Trim(), 15, ' ', true);
                                }
                                else
                                {
                                    //-- Si es jurídica, se toma el nombre de la empresa
                                    stringGrabar += "N" + new string(' ', 3);
                                    stringGrabar += utilidades.FormateoString2(registro.NIS.ToUpper().Trim(), 20, ' ', true);
                                    stringGrabar += utilidades.FormateoString2(registro.NJS.ToUpper().Trim(), 75, ' ', true);
                                }
                            }
                            else
                            {
                                //-- Busca información en la base de datos sobre este cliente
                                var dataTemporal = ConsultarIVECHCajaTemporalPorCliente(registro.CLT.Trim());
                                if (dataTemporal != null && dataTemporal.String.ToUpper() != "ERROR")
                                {
                                    stringGrabar += utilidades.FormateoString2(dataTemporal.String, 100, ' ', true);
                                    grabar = true;
                                }
                                else
                                {
                                    stringGrabar += utilidades.FormateoString2("", 76, ' ', true);
                                    grabar = false;
                                }
                            }
                        }

                        // Moneda y valor de transacción
                        stringGrabar += registro.MON == 0 ? "GTQ" : "USD";
                        stringGrabar += utilidades.FormateoString2(utilidades.FormateoMontos2(registro.VAL.ToString()), 14, ' ', true);
                        stringGrabar += utilidades.FormateoString2(utilidades.FormateoMontos2((registro.VAL / registro.BGU).ToString()), 14, ' ', true);

                        //-- Fondos
                        string origenFondos = !string.IsNullOrEmpty(registro.ORI) ? registro.ORI.Replace("/", "") : "";

                        //-- Pago y detalles de la transacción
                        int pago = registro.PAG == 5 ? 4 : registro.PAG;
                        if (registro.TPS == "B")
                        {
                            stringGrabar += "3";
                            stringGrabar += utilidades.FormateoString2("REGISTROS CONTABLES BANCO INTERNACIONAL S.A., DETALLE: " + origenFondos.Trim(), 500, ' ', true);
                        }
                        else
                        {
                            stringGrabar += pago.ToString();
                            stringGrabar += utilidades.FormateoString2(origenFondos.Trim(), 500, ' ', true);
                        }

                        //-- String final a grabar
                        if (grabar)
                        {
                            fileWriter.WriteLine(utilidades.QuitoTildes(stringGrabar));
                        }
                        else
                        {
                            cantidadRegsDetalleERROR++;
                        }
                        cantidadRegsDetalleOK++;
                    }
                }

                // Leer el archivo como arreglo de bytes y eliminarlo después
                byte[] fileBytes = File.ReadAllBytes(filePath);
                File.Delete(filePath);

                return fileBytes;
            }
            catch (Exception ex)
            {
                throw new Exception(" Error en TransaccionesClientes " + ex.Message);
            }
        }



        private int InsertarCHCajaTemporal(int cliente, string cadena, int dia, int mes, int ano)
        {
            string query = $"INSERT into IVE_CH_CAJA_TEMPORAL Values ({cliente}, '{cadena}', {dia}, {mes}, {ano} );";
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

        private int EliminaCHCajaTemporal()
        {
            string query = "DELETE FROM IVE_CH_CAJA_TEMPORAL";
            int filasAfectadas = 0;
            try
            {
                filasAfectadas = _dbHelper.ExecuteNonQuery(query);
            }
            catch (Exception ex)
            {
                throw new Exception(" Error al eliminar EliminaCHCajaTemporal " + ex.Message);
            }

            return filasAfectadas;
        }


        private List<DTO_IVECHCajaArchivos> ConsultarIVECHCajaPorRangoFechas(int fechaInicial, int fechaFinal)
        {
            List<DTO_IVECHCajaArchivos> listaDatos = new List<DTO_IVECHCajaArchivos>();

            // Extraer año y mes de las fechas inicial y final
            int anioInicial = fechaInicial / 10000;
            int mesInicial = (fechaInicial / 100) % 100;
            int anioFinal = fechaFinal / 10000;
            int mesFinal = (fechaFinal / 100) % 100;

            string query = @"
                SELECT * 
                FROM IVE_CH_CAJA_Archivos 
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
                    listaDatos.Add(new DTO_IVECHCajaArchivos
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

        private List<DTO_IVECHClientesCaja> ConsultarClientesCHCajaTemporal(int fechaInicial, int fechaFinal)
        {
            List<DTO_IVECHClientesCaja> resultado = new List<DTO_IVECHClientesCaja>();
            string query = $" Select " +
                           "   Distinct Clt as Cliente, " +
                           "   NombreCliente as Nombre, " +
                           "   TipoCliente as Tipo " +
                           " from " +
                           "   VChCaja  " +
                           "   inner join DWCLIENTE on CLT = COD_CLIENTE " +
                           " where " +
                           "   (Fec between " + fechaInicial + " and " + fechaFinal + ") "+
                           " and Clt <> '0' " +
                           " and Cheq <> 0 ";
            try
            {
                DataTable dt = _dbHelper.ExecuteSelectCommand(query);

                foreach (DataRow row in dt.Rows)
                {
                    resultado.Add(new DTO_IVECHClientesCaja
                    {
                        Cliente = int.Parse(row["Cliente"].ToString()),
                        Nombre = row["Nombre"].ToString(),
                        Tipo = int.Parse(row["Tipo"].ToString()),
                    });
                }
            }catch(Exception ex){
                throw new Exception("Error al consultar ConsultarClientesCHCajaTemporal " + ex.Message);
            }        
            return resultado;
        }

        private DTO_IVECHCajaTemporal ConsultarIVECHCajaTemporalPorCliente(string codigoCliente)
        {
            DTO_IVECHCajaTemporal resultado = new DTO_IVECHCajaTemporal();
            string query = $" SELECT * FROM IVE_CH_CAJA_TEMPORAL WHERE Cliente = { codigoCliente } ";

            try
            {
                DataTable dt = _dbHelper.ExecuteSelectCommand(query);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    resultado = new DTO_IVECHCajaTemporal
                    {
                        Cliente = float.Parse(row["Cliente"].ToString()),
                        String = row["String"].ToString(),
                        Dia = int.Parse(row["Dia"].ToString()),
                        Mes = int.Parse(row["Mes"].ToString()),
                        Ano = int.Parse(row["Ano"].ToString())
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al consultar ConsultarIVECHCajaTemporalPorCliente " + ex.Message);
            }
            return resultado;
        }

        private List<DTO_VCHCaja> ConsultarVCHCaja(int fechaInicial, int fechaFinal)
        {            
            List<DTO_VCHCaja> resultado = new List<DTO_VCHCaja>();
            string query = $" SELECT * FROM VChCaja WHERE FEC Between {fechaInicial} and {fechaFinal} order by fec";

            try
            {
                DataTable dt = _dbHelper.ExecuteSelectCommand(query);

                foreach (DataRow row in dt.Rows)
                {
                    resultado.Add(new DTO_VCHCaja
                    {
                        IU = int.TryParse(row["IU"].ToString(), out var iu) ? iu : (int?)null,
                        Sol = row["Sol"].ToString(),
                        Suc = int.TryParse(row["Suc"].ToString(), out var suc) ? suc : (int?)null,
                        Cheq = row["Cheq"].ToString(),
                        FEC = int.TryParse(row["FEC"].ToString(), out var fec) ? fec : 0,
                        MON = int.TryParse(row["MON"].ToString(), out var mon) ? mon : (int?)null,
                        CLT = row["CLT"].ToString(),
                        TPB = row["TPB"].ToString(),
                        IPB = row["IPB"].ToString(),
                        NOB = row["NOB"].ToString(),
                        NIB = row["NIB"].ToString(),
                        NJB = row["NJB"].ToString(),
                        PAB = row["PAB"].ToString(),
                        SAB = row["SAB"].ToString(),
                        ACB = row["ACB"].ToString(),
                        PNB = row["PNB"].ToString(),
                        SNB = row["SNB"].ToString(),
                        TPS = row["TPS"].ToString(),
                        IPS = row["IPS"].ToString(),
                        NOS = row["NOS"].ToString(),
                        NIS = row["NIS"].ToString(),
                        NJS = row["NJS"].ToString(),
                        PAS = row["PAS"].ToString(),
                        SAS = row["SAS"].ToString(),
                        ACS = row["ACS"].ToString(),
                        PNS = row["PNS"].ToString(),
                        SNS = row["SNS"].ToString(),
                        PAG = int.TryParse(row["PAG"].ToString(), out var pag) ? pag : 0,
                        ORI = row["ORI"].ToString(),
                        VAL = decimal.TryParse(row["VAL"].ToString(), out var val) ? val : (decimal?)null,
                        TC = decimal.TryParse(row["TC"].ToString(), out var tc) ? tc : (decimal?)null,
                        BGU = decimal.TryParse(row["BGU"].ToString(), out var bgu) ? bgu : (decimal?)null
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al consultar ConsultarVCHCaja " + ex.Message);
            }
            
            return resultado;
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
    }
}