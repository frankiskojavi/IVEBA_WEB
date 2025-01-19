using IVEBA_API_Rest.Helpers;
using IVEBA_API_Rest.Models.IVE17DV;
using IVEBA_API_Rest.Models.IVE21TRF;
using IVEBA_API_Rest.Models.IVECH;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace IVEBA_API_Rest.Services.IVE21TRF
{
    public class IVE21TRFHelperService : IIVE21TRFHelperService
    {
        private readonly DbHelper _dbHelper;
        //private readonly PP 

        private int contadorNit = 0;
        private int cantidadRegsDetalleOK = 0;
        private int cantidadRegsDetalleERROR = 0;
        private List<DTO_DWCliente> clientesDW = new List<DTO_DWCliente>();
        private List<DTO_UbicacionGeografica> ubicacionesGeogragricas = new List<DTO_UbicacionGeografica>();
        private List<DTO_IVETRF21Temporal> listaIVE21TemporalGlobal = new List<DTO_IVETRF21Temporal>();
        public IVE21TRFHelperService(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
            //= new PP();
        }

        public async Task<DTO_IVE21TRFResponse> GeneracionArchivoIVE21TRF(int fechaInicial, int fechaFinal, bool archivoDefinitivo)
        {
            DTO_IVE21TRFResponse response = new DTO_IVE21TRFResponse();
            string filePath = Path.Combine(Path.GetTempPath(), "archivoGenerado.txt");
            string datosPersona = "";
            string datosEmpresa = "";
            int cantidadRegistrosOK = 0;
            int año = 0;
            int mes = 0;
            StringBuilder logErrores = new StringBuilder();

            try
            {
                año = int.Parse(fechaInicial.ToString().Substring(0, 4));
                mes = int.Parse(fechaInicial.ToString().Substring(4, 2));
                // Verificar si se debe generar el archivo definitivo
                if (archivoDefinitivo)
                {
                    List<DTO_IVETRF21Archivos> registros = ConsultarIVETRF21(año, mes);

                    // Verifica si hay un archivo existente con la fecha enviada, de ser asi, devuelve la información ya existente para no volver a generarla.
                    if (registros.Count > 0)
                    {
                        //Existe archivo, devuelve archivo generado.
                        using (StreamWriter archivo = new StreamWriter(filePath))
                        {
                            foreach (var registro in registros)
                            {
                                string stringGrabar = $"{registro.String}";
                                stringGrabar = QuitoTildes(stringGrabar);
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
                }
                // Genera nueva información                
                TruncaIVETRF21Temporal();
                List<DTO_IVETRF21Clientes> listaClientes = ConsultarIVETRF21ClientesPorFecha(año, mes);
                string codigosClientes = string.Join(",", listaClientes.Select(cliente => cliente.Cliente.ToString()));

                //Consultas por unica vez en cada proceso para no recargar la BD
                clientesDW = ConsultarDWClienteTodos(codigosClientes);
                ubicacionesGeogragricas = ConsultarUbicacionesGeograficas();
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
                listaIVE21TemporalGlobal = ConsultarIVETRF21Temporal();
                response.archivoTXTOk = TransaccionesClientes(archivoDefinitivo, filePath, año, mes);


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

                // Buscar cliente en la lista
                List<DTO_DWCliente> clientes = clientesDW.Where(x => x.CodCliente == cliente).ToList();

                if (clientes.Count == 0)
                    return false;

                var clienteData = clientes.First(); // Se asume un solo cliente por ID
                var stringArmado = "I&&";
                string orden = string.Empty;
                string munisib = "  "; // Valor predeterminado

                // Construcción de identificación y tipo de documento
                switch (clienteData.TipoIdentificacion)
                {
                    case 1: // Cédula
                    case 22:
                        orden = clienteData.Identificacion.Length >= 3
                            ? clienteData.Identificacion.Substring(0, 3)
                            : clienteData.Identificacion.PadRight(3, ' ');

                        if (orden.Length >= 2 && orden[1] == '0')
                        {
                            orden = orden[0] + orden[2].ToString();
                        }

                        orden = FormateoString(orden, 3, ' ', true);

                        // Recuperar información de la ubicación geográfica
                        var ubicacion = ubicacionesGeogragricas.FirstOrDefault(x => x.UbicacionGeoId == clienteData.IdentUbicacion);

                        if (ubicacion != null && !string.IsNullOrEmpty(ubicacion.CodSibMuni))
                        {
                            munisib = ubicacion.CodSibMuni;
                        }

                        if (string.IsNullOrEmpty(munisib) || munisib == "00")
                        {
                            munisib = "01"; // Valor por defecto
                        }

                        stringArmado += "C&&";
                        stringArmado += orden + "&&";
                        stringArmado += FormateoString(clienteData.Identificacion.Length > 5
                            ? clienteData.Identificacion.Substring(5).PadRight(20)
                            : clienteData.Identificacion.PadRight(20), 20, ' ', true) + "&&";
                        stringArmado += munisib + "&&";
                        break;

                    case 2: // Partida
                        orden = FormateoString("", 3, ' ', true);
                        stringArmado += "O&&";
                        stringArmado += orden + "&&";
                        stringArmado += FormateoString(clienteData.Identificacion, 20, ' ', true) + "&&";
                        stringArmado += munisib + "&&";
                        break;

                    case 4: // Pasaporte
                        orden = FormateoString("", 3, ' ', true);
                        stringArmado += "P&&";
                        stringArmado += orden + "&&";
                        stringArmado += FormateoString(clienteData.Identificacion, 20, ' ', true) + "&&";
                        stringArmado += munisib + "&&";
                        break;

                    case 26: // DPI
                        orden = "   ";
                        stringArmado += "D&&";
                        stringArmado += orden + "&&";
                        stringArmado += FormateoString(clienteData.Identificacion, 20, ' ', true) + "&&";
                        stringArmado += munisib + "&&";
                        break;
                }

                // Construcción de apellidos y nombres
                /*
                stringArmado += FormateoString(QuitoTildes(clienteData.Apellido1?.ToUpper() ?? ""), 15, ' ', true) + "&&";
                stringArmado += FormateoString(QuitoTildes(clienteData.Apellido2?.ToUpper() ?? ""), 15, ' ', true) + "&&";
                stringArmado += FormateoString(QuitoTildes(clienteData.ApellidoCasada?.ToUpper() ?? ""), 15, ' ', true) + "&&";
                stringArmado += FormateoString(QuitoTildes(clienteData.Nombre1?.ToUpper() ?? ""), 15, ' ', true) + "&&";
                stringArmado += FormateoString(QuitoTildes(clienteData.Nombre2?.ToUpper() ?? ""), 30, ' ', true);
                */
                stringArmado += FormateoString(QuitoTildes((clienteData.Apellido1?.ToUpper() ?? "").PadRight(15)).Substring(0, 15), 15, ' ', true) + "&&";
                stringArmado += FormateoString(QuitoTildes((clienteData.Apellido2?.ToUpper() ?? "").PadRight(15)).Substring(0, 15), 15, ' ', true) + "&&";
                stringArmado += FormateoString(QuitoTildes((clienteData.ApellidoCasada?.ToUpper() ?? "").PadRight(15)).Substring(0, 15), 15, ' ', true) + "&&";
                stringArmado += FormateoString(QuitoTildes((clienteData.Nombre1?.ToUpper() ?? "").PadRight(15)).Substring(0, 15), 15, ' ', true) + "&&";
                stringArmado += FormateoString(QuitoTildes((clienteData.Nombre2?.ToUpper() ?? "").PadRight(30)).Substring(0, 30), 30, ' ', true);


                // Resultado final
                stringDatos = QuitoTildes(stringArmado);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error en ProcesoFisicos: " + ex.Message, ex);
            }
        }
        private bool ProcesoJuridicos(int cliente, out string stringDatos)
        {
            try
            {
                stringDatos = string.Empty;
                var stringArmado = "";
                string nitEmpresa = "";

                List<DTO_DWCliente> clientes = clientesDW.Where(x => x.CodCliente.Equals(cliente)).ToList();

                if (clientes.Count == 0)
                    return false;

                var varEmpresa = clientes.First(); // Suponiendo un solo cliente por ID                

                if (varEmpresa.Nit.Equals("") || varEmpresa.Nit.Equals("0"))
                {
                    nitEmpresa = "";
                }
                else
                {
                    nitEmpresa = varEmpresa.Nit;
                }

                if (nitEmpresa.Equals(""))
                {
                    if (varEmpresa.TipoIdentificacion.Equals("8"))
                    {
                        nitEmpresa = varEmpresa.Identificacion.Trim();
                    }
                    else
                    {
                        nitEmpresa = "SINNIT";
                        contadorNit++;
                    }
                    if (nitEmpresa.Equals("0") || nitEmpresa.Equals("")) nitEmpresa = "SINNIT";
                }

                // Asigna un NIT específico si es el cliente con CodCliente "10"
                if (cliente == 10) nitEmpresa = "1205544";

                string nombreCliente = QuitoTildes(varEmpresa.NombreCliente?.Trim() ?? string.Empty);

                stringArmado = "J" + "&&";
                stringArmado = stringArmado + "N" + "&&";                
                stringArmado = stringArmado + "   " + "&&";
                stringArmado = stringArmado + FormateoString(QuitoCaracter(nitEmpresa.Trim()), 20,' ', true) + "&&";
                stringArmado = stringArmado + "  " + "&&";

                // Construcción segura de las partes del nombre
                stringArmado += FormateoString(nombreCliente.PadRight(15).Substring(0, 15), 15, ' ', true) + "&&";
                stringArmado += FormateoString((nombreCliente.Length > 15 ? nombreCliente.Substring(15).PadRight(15) : string.Empty.PadRight(15)).Substring(0, 15), 15, ' ', true) + "&&";
                stringArmado += FormateoString((nombreCliente.Length > 30 ? nombreCliente.Substring(30).PadRight(15) : string.Empty.PadRight(15)).Substring(0, 15), 15, ' ', true) + "&&";
                stringArmado += FormateoString((nombreCliente.Length > 45 ? nombreCliente.Substring(45).PadRight(15) : string.Empty.PadRight(15)).Substring(0, 15), 15, ' ', true) + "&&";
                stringArmado += FormateoString((nombreCliente.Length > 60 ? nombreCliente.Substring(60).PadRight(30) : string.Empty.PadRight(30)).Substring(0, 30), 30, ' ', true);

                stringDatos = stringArmado;
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error en ProcesoJuridicos: " + ex.Message, ex);
            }
        }

        private byte[] TransaccionesClientes(bool tipoArchivo, string filePath, int año, int mes)
        {
            string StringGrabar = "";
            bool CltOrd = false;
            bool CltBen = false;
            string Pais = "";
            string SucursalTrn = "";
            string DeptoD = "";
            string DeptoO = "";
            string NombreTmp = "";


            string OTIPOID = "";
            string OORDEN = "";
            string OID = "";
            string OMUNI = "";
            string OPAPE = "";
            string OSAPE = "";
            string OACAS = "";
            string OPNOM = "";
            string OSNOM = "";

            string BTIPOID = "";
            string BORDEN = "";
            string BID = "";
            string BMUNI = "";
            string BPAPE = "";
            string BSAPE = "";
            string BACAS = "";
            string BPNOM = "";
            string BSNOM = "";
            string OTIPOP = "";

            try
            {
                List<DTO_IVE21TRF> listaIVE21TRF = ConsultarIVE21TRFPorFecha(año, mes);                
                
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
                                    var listaIVE21Temporal = listaIVE21TemporalGlobal.Where(x => x.Cliente == float.Parse(registro.TRFOCUN));
                                    foreach (DTO_IVETRF21Temporal registro2 in listaIVE21Temporal)
                                    {
                                        StringGrabar = "";
                                        StringGrabar += registro.TRFFECHA.ToString("yyyyMMdd") + "&&";
                                        StringGrabar += "2" + "&&";
                                        StringGrabar += "E" + "&&";

                                        if (registro2.String.Substring(0, 1) == "I")
                                        {
                                            StringGrabar += FormateoString(registro2.String.Trim(), 135, ' ', true) + "&&";
                                        }
                                        else
                                        {
                                            StringGrabar += FormateoString(registro2.String.Trim(), 135, ' ', true) + "&&";
                                        }
                                        CltOrd = true;
                                        //fileWriter.WriteLine(StringGrabar);
                                    }
                                }
                                else
                                {
                                    CltOrd = false;
                                }

                                if (CltOrd == false)
                                {
                                    StringGrabar = "";
                                    StringGrabar += registro.TRFFECHA.ToString("yyyyMMdd") + "&&";
                                    StringGrabar += "2" + "&&";
                                    StringGrabar += "E" + "&&";
                                    OTIPOP = registro.TRFOTPER.Trim();
                                    if (OTIPOP == "I")
                                    {
                                        OTIPOID = registro.TRFOTID.Trim();
                                        if (registro.TRFOTID == "C")
                                        {
                                            OORDEN = registro.TRFOORD.Trim();
                                            OID = registro.TRFODOC.Trim();
                                            OMUNI = registro.TRFOMUN.Trim();
                                            OPAPE = registro.TRFOAPE1.Trim();
                                            OSAPE = registro.TRFOAPE2.Trim();
                                            OACAS = registro.TRFOAPEC.Trim();
                                            OPNOM = registro.TRFONOM1.Trim();
                                            OSNOM = registro.TRFONOM2.Trim();
                                        }else
                                        {
                                            OORDEN = "   ";
                                            OID = registro.TRFODOC.Trim();
                                            OMUNI = "  ";
                                            OPAPE = registro.TRFOAPE1.Trim();
                                            OSAPE = registro.TRFOAPE2.Trim();
                                            OACAS = registro.TRFOAPEC.Trim();
                                            OPNOM = registro.TRFONOM1.Trim();
                                            OSNOM = registro.TRFONOM2.Trim();
                                        }                                                                                

                                        StringGrabar += FormateoString(OTIPOP, 1, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OTIPOID, 1, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OORDEN, 3, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OID, 20, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OMUNI, 2, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OPAPE, 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OSAPE, 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OACAS, 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OPNOM, 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OSNOM, 30, ' ', true) + "&&";
                                    }
                                    else
                                    {
                                        OTIPOID = registro.TRFOTID.Trim();
                                        OORDEN = "   ";
                                        OID = registro.TRFODOC.Trim();
                                        OMUNI = "  ";
                                        OPAPE = registro.TRFOAPE1.Trim();
                                        OTIPOP = "";

                                        StringGrabar += FormateoString(OTIPOP, 1, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OTIPOID, 1, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OORDEN, 3, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OID, 20, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OMUNI, 2, ' ', true) + "&&";
                                        /*
                                        StringGrabar += FormateoString(QuitoTildes(OPAPE.Substring(1, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes(OPAPE.Substring(16, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes(OPAPE.Substring(31, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes(OPAPE.Substring(46, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes(OPAPE.Substring(61, 30)), 30, ' ', true) + "&&";
                                        */
                                        StringGrabar += FormateoString(QuitoTildes((OPAPE.Length > 1 ? OPAPE.Substring(1).PadRight(15) : string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes((OPAPE.Length > 16 ? OPAPE.Substring(16).PadRight(15) : string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes((OPAPE.Length > 31 ? OPAPE.Substring(31).PadRight(15) : string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes((OPAPE.Length > 46 ? OPAPE.Substring(46).PadRight(15) : string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes((OPAPE.Length > 61 ? OPAPE.Substring(61).PadRight(30) : string.Empty.PadRight(30)).Substring(0, 30)), 30, ' ', true) + "&&";

                                    }
                                }

                                //****** ORIGEN : 

                                if (!string.IsNullOrEmpty(registro.IBANO))
                                {
                                    StringGrabar += FormateoString(registro.IBANO, 28, ' ', true) + "&&";
                                }
                                else
                                {
                                    StringGrabar += FormateoString(registro.TRFOCTA, 28, ' ', true) + "&&";
                                }

                                if (!string.IsNullOrEmpty(registro.TRFBCUN) && !registro.TRFBCUN.Equals("0"))
                                {                                    
                                    var listaTemporalBeneficiarios = listaIVE21TemporalGlobal.Where(x => x.Cliente == float.Parse(registro.TRFOCUN));

                                    if (listaTemporalBeneficiarios.Any())
                                    {
                                        foreach (var registro2 in listaTemporalBeneficiarios)
                                        {
                                            if (registro2.String.StartsWith("I"))
                                            {
                                                StringGrabar += FormateoString(registro2.String.Trim(), 135, ' ', true) + "&&";
                                            }
                                            else
                                            {
                                                StringGrabar += FormateoString(registro2.String.Trim(), 135, ' ', true) + "&&";
                                            }
                                            CltBen = true;
                                        }
                                    }
                                    else
                                    {
                                        CltBen = false;
                                    }
                                }
                                else
                                {
                                    CltBen = false;
                                }

                                if (CltBen == false)
                                {
                                    string BTIPOP = registro.TRFBTPER?.Trim();
                                    if (BTIPOP == "I")
                                    {
                                        BTIPOID = registro.TRFBTID.Trim();

                                        if (registro.TRFOTID == "C")
                                        {
                                            BORDEN = registro.TRFBORD.Trim();
                                            BID = registro.TRFODOC.Trim();
                                            BMUNI = registro.TRFOMUN.Trim();
                                            BPAPE = registro.TRFOAPE1.Trim();
                                            BSAPE = registro.TRFOAPE2.Trim();
                                            BACAS = registro.TRFOAPEC.Trim();
                                            BPNOM = registro.TRFONOM1.Trim();
                                            BSNOM = registro.TRFONOM2.Trim();
                                        }
                                        else
                                        {
                                            BORDEN = "   ";
                                            BID = registro.TRFODOC.Trim();
                                            BMUNI = "  ";
                                            BPAPE = registro.TRFOAPE1.Trim();
                                            BSAPE = registro.TRFOAPE2.Trim();
                                            BACAS = registro.TRFOAPEC.Trim();
                                            BPNOM = registro.TRFONOM1.Trim();
                                            BSNOM = registro.TRFONOM2.Trim();
                                        }                                        

                                        StringGrabar += FormateoString(BTIPOP, 1, ' ', true) + "&&";
                                        StringGrabar += FormateoString(BTIPOID, 1, ' ', true) + "&&";
                                        StringGrabar += FormateoString(BORDEN, 3, ' ', true) + "&&";
                                        StringGrabar += FormateoString(BID, 20, ' ', true) + "&&";
                                        StringGrabar += FormateoString(BMUNI, 2, ' ', true) + "&&";
                                        StringGrabar += FormateoString(BPAPE, 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(BSAPE, 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(BACAS, 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(BPNOM, 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(BSNOM, 30, ' ', true) + "&&";
                                    }
                                    else
                                    {
                                        BTIPOID = registro.TRFOTID.Trim();
                                        BORDEN = "   ";
                                        BID = registro.TRFODOC.Trim();
                                        BMUNI = "  ";
                                        BPAPE = registro.TRFOAPE1.Trim();
                                        BTIPOP = "";

                                        StringGrabar += FormateoString(BTIPOP, 1, ' ', true) + "&&";
                                        StringGrabar += FormateoString(BTIPOID, 1, ' ', true) + "&&";
                                        StringGrabar += FormateoString(BORDEN, 3, ' ', true) + "&&";
                                        StringGrabar += FormateoString(BID, 20, ' ', true) + "&&";
                                        StringGrabar += FormateoString(BMUNI, 2, ' ', true) + "&&";
                                        /*
                                        StringGrabar += FormateoString(QuitoTildes(BPAPE.Substring(0, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes(BPAPE.Substring(16, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes(BPAPE.Substring(31, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes(BPAPE.Substring(46, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes(BPAPE.Substring(61, 30)), 30, ' ', true) + "&&";
                                        */
                                        StringGrabar += FormateoString(QuitoTildes((BPAPE.Length >= 15 ? BPAPE.Substring(0, 15) : BPAPE.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes((BPAPE.Length >= 31 ? BPAPE.Substring(16, 15) : string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes((BPAPE.Length >= 46 ? BPAPE.Substring(31, 15) : string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes((BPAPE.Length >= 61 ? BPAPE.Substring(46, 15) : string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes((BPAPE.Length > 61 ? BPAPE.Substring(61).PadRight(30) : string.Empty.PadRight(30)).Substring(0, 30)), 30, ' ', true) + "&&";

                                    }
                                }


                                //****** BENEFICIARIO : 

                                if (!string.IsNullOrEmpty(registro.IBANB))
                                {
                                    StringGrabar += FormateoString(registro.IBANB, 28, ' ', true) + "&&";
                                }
                                else
                                {
                                    StringGrabar += FormateoString(registro.TRFBCTA, 28, ' ', true) + "&&";
                                }

                                if (registro.TRFBBCO == 0)
                                {
                                    StringGrabar += FormateoString("117", 5, ' ', true) + "&&";
                                }
                                else
                                {
                                    StringGrabar += FormateoString(registro.TRFBBCO.ToString(), 5, ' ', true) + "&&";
                                }

                                Pais = !string.IsNullOrEmpty(registro.TRFPAIS) ? registro.TRFPAIS : "GT";
                                DeptoO = !string.IsNullOrEmpty(registro.TRFODEPT) ? registro.TRFODEPT : "01";
                                DeptoD = !string.IsNullOrEmpty(registro.TRFDDEPT) ? registro.TRFDDEPT : "01";

                                StringGrabar += FormateoString(registro.TRFNUM, 20, ' ', true) + "&&";
                                StringGrabar += FormateoString(Pais, 2, ' ', true) + "&&";
                                StringGrabar += FormateoString(DeptoO, 2, ' ', true) + "&&";
                                StringGrabar += FormateoString(DeptoD, 2, ' ', true) + "&&";
                                SucursalTrn = !string.IsNullOrEmpty(registro.TRFBRN) ? registro.TRFBRN : "0";
                                if (double.TryParse(SucursalTrn, out double sucursal) && sucursal > 100)
                                {
                                    SucursalTrn = "0";
                                }

                                StringGrabar += FormateoString(SucursalTrn, 10, ' ', true) + "&&";
                                StringGrabar += FormateoString(FormateoMontos(registro.TRFMNT.ToString()), 14, ' ', true) + "&&";

                                if (registro.TRFCCY == "QTZ")
                                {
                                    StringGrabar += "GTQ" + "&&";
                                }
                                else
                                {
                                    StringGrabar += FormateoString(registro.TRFCCY, 3, ' ', true) + "&&";
                                }

                                StringGrabar += FormateoString(FormateoMontos(registro.TRFMNTD.ToString()), 14, ' ', true) + "&&";                                
                                break;
                            case "3": // TRANSFERENCIAS SWIFT
                                switch (registro.TRFTRAN)
                                {
                                    case "E": // TRANSFERENCIAS SWIFT ENVIADAS
                                        if (!string.IsNullOrEmpty(registro.TRFOCUN) && registro.TRFOCUN != "0")
                                        {                                            
                                            var listaTemporal = listaIVE21TemporalGlobal.Where(x => x.Cliente == float.Parse(registro.TRFOCUN));
                                            if (listaTemporal.Any())
                                            {
                                                foreach (var temporal in listaTemporal)
                                                {
                                                    StringGrabar = "";
                                                    StringGrabar += registro.TRFFECHA.ToString("yyyyMMdd") + "&&";
                                                    StringGrabar += "3" + "&&";
                                                    StringGrabar += "E" + "&&";
                                                    StringGrabar += FormateoString(temporal.String.Trim(), 135, ' ', true) + "&&";                                                    
                                                }
                                            }
                                        }
                                        else
                                        {
                                            StringGrabar = "";
                                            StringGrabar += registro.TRFFECHA.ToString("yyyyMMdd") + "&&";
                                            StringGrabar += "3" + "&&";
                                            StringGrabar += "E" + "&&";
                                            StringGrabar += "J" + "&&";
                                            StringGrabar += "N" + "&&";
                                            StringGrabar += FormateoString("   ", 3,' ', true) + "&&";
                                            StringGrabar += FormateoString("1205544", 20, ' ', true) + "&&";
                                            StringGrabar += "  " + "&&";

                                            NombreTmp = "BANCO INTERNACIONAL, S.A.";
                                            /*
                                            StringGrabar += FormateoString(QuitoTildes(NombreTmp.Substring(1, 15)), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes(NombreTmp.Substring(16, 15)), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes(NombreTmp.Substring(31, 15)), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes(NombreTmp.Substring(46, 15)), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes(NombreTmp.Substring(61, 30)), 30, ' ', true) + "&&";
                                            */
                                            StringGrabar += FormateoString(QuitoTildes((NombreTmp.Length > 1 ? NombreTmp.Substring(1).PadRight(15) : string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes((NombreTmp.Length > 16 ? NombreTmp.Substring(16).PadRight(15) : string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes((NombreTmp.Length > 31 ? NombreTmp.Substring(31).PadRight(15) : string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes((NombreTmp.Length > 46 ? NombreTmp.Substring(46).PadRight(15) : string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes((NombreTmp.Length > 61 ? NombreTmp.Substring(61).PadRight(30) : string.Empty.PadRight(30)).Substring(0, 30)), 30, ' ', true) + "&&";

                                        }

                                        if (registro.IBANO != null)
                                        {
                                            StringGrabar += FormateoString(registro.IBANO, 28, ' ', true) + "&&";
                                        }
                                        else
                                        {
                                            StringGrabar += FormateoString(registro.TRFOCTA, 28, ' ', true) + "&&";
                                        }

                                        string BTIPOP = registro.TRFBTPER?.Trim();
                                        if (BTIPOP == "I")
                                        {
                                            BTIPOID = registro.TRFBTID?.Trim();                                            
                                            if (BTIPOID == "C")
                                            {
                                                BORDEN = registro.TRFBORD?.Trim();
                                                BID = registro.TRFBDOC?.Trim();
                                                BMUNI = registro.TRFBMUN?.Trim();
                                                BPAPE = registro.TRFBAPE1?.Trim();
                                                BSAPE = registro.TRFBAPE2?.Trim();
                                                BACAS = registro.TRFBAPEC?.Trim();
                                                BPNOM = registro.TRFBNOM1?.Trim();
                                                BSNOM = registro.TRFBNOM2?.Trim();
                                            }
                                            else
                                            {
                                                BORDEN = "   ";
                                                BID = registro.TRFBDOC?.Trim();
                                                BMUNI = "  ";
                                                BPAPE = registro.TRFBAPE1?.Trim();
                                                BSAPE = registro.TRFBAPE2?.Trim();
                                                BACAS = registro.TRFBAPEC?.Trim();
                                                BPNOM = registro.TRFBNOM1?.Trim();
                                                BSNOM = registro.TRFBNOM2?.Trim();
                                            }

                                            StringGrabar += FormateoString(BTIPOP, 1, ' ', true) + "&&";
                                            StringGrabar += FormateoString(BTIPOID, 1, ' ', true) + "&&";
                                            StringGrabar += FormateoString(BORDEN, 3, ' ', true) + "&&";
                                            StringGrabar += FormateoString(BID, 20, ' ', true) + "&&";
                                            StringGrabar += FormateoString(BMUNI, 2, ' ', true) + "&&";
                                            StringGrabar += FormateoString(BPAPE, 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(BSAPE, 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(BACAS, 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(BPNOM, 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(BSNOM, 30, ' ', true) + "&&";
                                        }
                                        else
                                        {
                                            BTIPOID = registro.TRFBTID?.Trim();
                                            BORDEN = "   ";
                                            BID = registro.TRFBDOC?.Trim();
                                            BMUNI = "  ";
                                            BPAPE = registro.TRFBAPE1?.Trim();
                                            StringGrabar += FormateoString(BTIPOP, 1, ' ', true) + "&&";
                                            StringGrabar += FormateoString(BTIPOID, 1, ' ', true) + "&&";
                                            StringGrabar += FormateoString(BORDEN, 3, ' ', true) + "&&";
                                            StringGrabar += FormateoString(BID, 20, ' ', true) + "&&";
                                            StringGrabar += FormateoString(BMUNI, 2, ' ', true) + "&&";
                                            /*
                                            StringGrabar += FormateoString(QuitoTildes(BPAPE?.Substring(1, 15)), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes(BPAPE?.Substring(16, 15)), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes(BPAPE?.Substring(31, 15)), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes(BPAPE?.Substring(46, 15)), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes(BPAPE?.Substring(61, 30)), 30, ' ', true) + "&&";
                                            */
                                            StringGrabar += FormateoString(QuitoTildes((BPAPE?.Length > 1 ? BPAPE.Substring(1).PadRight(15) : string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes((BPAPE?.Length > 16 ? BPAPE.Substring(16).PadRight(15) : string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes((BPAPE?.Length > 31 ? BPAPE.Substring(31).PadRight(15) : string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes((BPAPE?.Length > 46 ? BPAPE.Substring(46).PadRight(15) : string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes((BPAPE?.Length > 61 ? BPAPE.Substring(61).PadRight(30) : string.Empty.PadRight(30)).Substring(0, 30)), 30, ' ', true) + "&&";

                                        }

                                        if (!string.IsNullOrEmpty(registro.IBANB))
                                        {
                                            StringGrabar += FormateoString(registro.IBANB, 28, ' ', true) + "&&";
                                        }
                                        else
                                        {
                                            StringGrabar += FormateoString(registro.TRFBCTA, 28, ' ', true) + "&&";
                                        }

                                        if (registro.TRFBBCO == 0)
                                        {
                                            StringGrabar += FormateoString("", 5, ' ', true) + "&&";
                                        }
                                        else
                                        {
                                            StringGrabar += FormateoString(registro.TRFBBCO.ToString(), 5, ' ', true) + "&&";
                                        }

                                        Pais = registro.TRFPAIS?.Trim();

                                        StringGrabar += FormateoString(registro.TRFNUM, 20, ' ', true) + "&&";

                                        if (Pais == "GT")
                                        {
                                            Pais = "US";
                                        }

                                        StringGrabar += FormateoString(Pais, 2, ' ', true) + "&&";

                                        string DeptoOrigen = !string.IsNullOrEmpty(registro.DepartamentoAgencia.ToString()) ? registro.DepartamentoAgencia.ToString() : "01";
                                        if (DeptoOrigen == "" || DeptoOrigen == "0")
                                        {
                                            DeptoOrigen = "01";
                                        }

                                        SucursalTrn = registro.TRFBRN;
                                        if (string.IsNullOrEmpty(SucursalTrn))
                                        {
                                            SucursalTrn = "0";
                                        }

                                        if (int.Parse(SucursalTrn) > 100)
                                        {
                                            SucursalTrn = "0";
                                        }

                                        StringGrabar += FormateoString(DeptoOrigen.PadLeft(2, '0'), 2, ' ', true) + "&&";

                                        if (Pais == "US")
                                        {
                                            DeptoD = !string.IsNullOrEmpty(registro.TRFDDEPT) ? registro.TRFDDEPT : "";
                                            if (DeptoD == "GT" || DeptoD == "US")
                                            {
                                                DeptoD = "FL";
                                            }
                                            StringGrabar += FormateoString(DeptoD, 2, ' ', true) + "&&";
                                        }
                                        else
                                        {
                                            StringGrabar += FormateoString("", 2, ' ', true) + "&&";
                                        }

                                        StringGrabar += FormateoString(SucursalTrn, 10, ' ', true) + "&&";
                                        StringGrabar += FormateoString(FormateoMontos(registro.TRFMNT.ToString()), 14, ' ', true) + "&&";

                                        if (registro.TRFCCY == "QTZ")
                                        {
                                            StringGrabar += "GTQ" + "&&";
                                        }
                                        else
                                        {
                                            StringGrabar += FormateoString(registro.TRFCCY, 3, ' ', true) + "&&";
                                        }

                                        StringGrabar += FormateoString(FormateoMontos(registro.TRFMNTD.ToString()), 14, ' ', true) + "&&";
                                        break;
                                    case "R": // SWIFT RECIBIDAS
                                        StringGrabar = "";
                                        StringGrabar += registro.TRFFECHA.ToString("yyyyMMdd") + "&&";
                                        StringGrabar += "3" + "&&";
                                        StringGrabar += "R" + "&&";

                                        OTIPOP = registro.TRFOTPER?.Trim();
                                        if (OTIPOP == "I")
                                        {
                                            OTIPOID = registro.TRFOTID.Trim();
                                            if (OTIPOID == "C")
                                            {
                                                OORDEN = registro.TRFOORD?.Trim();
                                                OID = registro.TRFODOC?.Trim();
                                                OMUNI = registro.TRFOMUN?.Trim();
                                                OPAPE = registro.TRFOAPE1?.Trim();
                                                OSAPE = registro.TRFOAPE2?.Trim();
                                                OACAS = registro.TRFOAPEC?.Trim();
                                                OPNOM = registro.TRFONOM1?.Trim();
                                                OSNOM = registro.TRFONOM2?.Trim();

                                                if (string.IsNullOrEmpty(OPNOM) && !string.IsNullOrEmpty(OSAPE))
                                                {
                                                    OPNOM = OSAPE;
                                                    OSAPE = "";
                                                }
                                            }
                                            else
                                            {
                                                OTIPOID = "O";
                                                OORDEN = "   ";
                                                OID = registro.TRFODOC?.Trim();
                                                OMUNI = "  ";
                                                OPAPE = registro.TRFOAPE1?.Trim();
                                                OSAPE = registro.TRFOAPE2?.Trim();
                                                OACAS = registro.TRFOAPEC?.Trim();
                                                OPNOM = registro.TRFONOM1?.Trim();
                                                OSNOM = registro.TRFONOM2?.Trim();

                                                if (string.IsNullOrEmpty(OPNOM) && !string.IsNullOrEmpty(OSAPE))
                                                {
                                                    OPNOM = OSAPE;
                                                    OSAPE = "";
                                                }
                                            }

                                            StringGrabar += FormateoString(OTIPOP, 1, ' ', true) + "&&";
                                            StringGrabar += FormateoString(OTIPOID, 1, ' ', true) + "&&";
                                            StringGrabar += FormateoString(OORDEN, 3, ' ', true) + "&&";
                                            StringGrabar += FormateoString(OID, 20, ' ', true) + "&&";
                                            StringGrabar += FormateoString(OMUNI, 2, ' ', true) + "&&";
                                            StringGrabar += FormateoString(OPAPE, 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(OSAPE, 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(OACAS, 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(OPNOM, 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(OSNOM, 30, ' ', true) + "&&";
                                        }
                                        else
                                        {
                                            OTIPOID = registro.TRFOTID?.Trim();
                                            OORDEN = "   ";
                                            OID = registro.TRFODOC?.Trim();
                                            OMUNI = "  ";
                                            OPAPE = registro.TRFOAPE1?.Trim();

                                            StringGrabar += FormateoString(OTIPOP, 1, ' ', true) + "&&";
                                            StringGrabar += FormateoString(OTIPOID, 1, ' ', true) + "&&";
                                            StringGrabar += FormateoString(OORDEN, 3, ' ', true) + "&&";
                                            StringGrabar += FormateoString(OID, 20, ' ', true) + "&&";
                                            StringGrabar += FormateoString(OMUNI, 2, ' ', true) + "&&";
                                            /*
                                            StringGrabar += FormateoString(QuitoTildes(OPAPE?.Substring(0, Math.Min(15, OPAPE.Length))), 15,' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes(OPAPE?.Substring(15, Math.Min(15, OPAPE.Length - 15))), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes(OPAPE?.Substring(30, Math.Min(15, OPAPE.Length - 30))), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes(OPAPE?.Substring(45, Math.Min(15, OPAPE.Length - 45))), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes(OPAPE?.Substring(60, Math.Min(30, OPAPE.Length - 60))), 30, ' ', true) + "&&";
                                            */
                                            StringGrabar += FormateoString(QuitoTildes((OPAPE?.Length >= 15 ? OPAPE.Substring(0, 15) : OPAPE?.PadRight(15) ?? string.Empty.PadRight(15))), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes((OPAPE?.Length >= 30 ? OPAPE.Substring(15, 15) : OPAPE?.Substring(15)?.PadRight(15) ?? string.Empty.PadRight(15))), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes((OPAPE?.Length >= 45 ? OPAPE.Substring(30, 15) : OPAPE?.Substring(30)?.PadRight(15) ?? string.Empty.PadRight(15))), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes((OPAPE?.Length >= 60 ? OPAPE.Substring(45, 15) : OPAPE?.Substring(45)?.PadRight(15) ?? string.Empty.PadRight(15))), 15, ' ', true) + "&&";
                                            StringGrabar += FormateoString(QuitoTildes((OPAPE?.Length > 60 ? OPAPE.Substring(60).PadRight(30) : string.Empty.PadRight(30))), 30, ' ', true) + "&&";

                                        }

                                        if (!string.IsNullOrEmpty(registro.IBANO))
                                        {
                                            StringGrabar += FormateoString(registro.IBANO, 28, ' ', true) + "&&";
                                        }
                                        else
                                        {
                                            StringGrabar += FormateoString(registro.TRFOCTA, 28, ' ', true) + "&&";
                                        }

                                        CltBen = false;
                                        if (!string.IsNullOrEmpty(registro.TRFOCUN) && registro.TRFOCUN != "0")
                                        {                                            
                                            var registrosTemporal = listaIVE21TemporalGlobal.Where(x => x.Cliente == float.Parse(registro.TRFOCUN));
                                            if (registrosTemporal.Any())
                                            {
                                                foreach (var temp in registrosTemporal)
                                                {
                                                    if (temp.String.StartsWith("I"))
                                                    {
                                                        StringGrabar += FormateoString(temp.String.Trim(), 135, ' ', true) + "&&";
                                                    }
                                                    else
                                                    {
                                                        StringGrabar += FormateoString(temp.String.Trim(), 135, ' ', true) + "&&";
                                                    }
                                                }
                                                CltBen = true;
                                            }
                                        }

                                        if (CltBen == false)
                                        {
                                            BTIPOP = registro.TRFBTPER?.Trim();
                                            if (BTIPOP == "I")
                                            {
                                                BTIPOID = registro.TRFBTID;
                                                if (BTIPOID == "C")
                                                {
                                                    BORDEN = registro.TRFBORD?.Trim();
                                                    BID = registro.TRFBDOC?.Trim();
                                                    BMUNI = registro.TRFBMUN?.Trim();
                                                    BPAPE = registro.TRFBAPE1?.Trim();
                                                    BSAPE = registro.TRFBAPE2?.Trim();
                                                    BACAS = registro.TRFBAPEC?.Trim();
                                                    BPNOM = registro.TRFBNOM1?.Trim();
                                                    BSNOM = registro.TRFBNOM2?.Trim();
                                                }
                                                else
                                                {
                                                    BORDEN = "   ";
                                                    BID = registro.TRFBDOC?.Trim();
                                                    BMUNI = "  ";
                                                    BPAPE = registro.TRFBAPE1?.Trim();
                                                    BSAPE = registro.TRFBAPE2?.Trim();
                                                    BACAS = registro.TRFBAPEC?.Trim();
                                                    BPNOM = registro.TRFBNOM1?.Trim();
                                                    BSNOM = registro.TRFBNOM2?.Trim();
                                                }

                                                StringGrabar += FormateoString(BTIPOP, 1, ' ', true) + "&&";
                                                StringGrabar += FormateoString(BTIPOID, 1, ' ', true) + "&&";
                                                StringGrabar += FormateoString(BORDEN, 3, ' ', true) + "&&";
                                                StringGrabar += FormateoString(BID, 20, ' ', true) + "&&";
                                                StringGrabar += FormateoString(BMUNI, 2, ' ', true) + "&&";
                                                StringGrabar += FormateoString(BPAPE, 15, ' ', true) + "&&";
                                                StringGrabar += FormateoString(BSAPE, 15, ' ', true) + "&&";
                                                StringGrabar += FormateoString(BACAS, 15, ' ', true) + "&&";
                                                StringGrabar += FormateoString(BPNOM, 15, ' ', true) + "&&";
                                                StringGrabar += FormateoString(BSNOM, 30, ' ', true) + "&&";
                                            }
                                            else
                                            {
                                                BTIPOID = registro.TRFBTID?.Trim() ?? "N";
                                                BORDEN = "   ";
                                                BID = registro.TRFBDOC?.Trim();
                                                if (string.IsNullOrEmpty(BID) || BID == "--") BID = "SINNIT";
                                                BMUNI = "  ";
                                                BPAPE = registro.TRFBAPE1?.Trim();

                                                StringGrabar += FormateoString(BTIPOP, 1, ' ', true) + "&&";
                                                StringGrabar += FormateoString(BTIPOID, 1, ' ', true) + "&&";
                                                StringGrabar += FormateoString(BORDEN, 3, ' ', true) + "&&";
                                                StringGrabar += FormateoString(BID, 20, ' ', true) + "&&";
                                                StringGrabar += FormateoString(BMUNI, 2, ' ', true) + "&&";
                                                /*
                                                StringGrabar += FormateoString(QuitoTildes(BPAPE?.Substring(0, Math.Min(15, BPAPE.Length))), 15, ' ', true) + "&&";
                                                StringGrabar += FormateoString(QuitoTildes(BPAPE?.Substring(15, Math.Min(15, BPAPE.Length - 15))), 15, ' ', true) + "&&";
                                                StringGrabar += FormateoString(QuitoTildes(BPAPE?.Substring(30, Math.Min(15, BPAPE.Length - 30))), 15, ' ', true) + "&&";
                                                StringGrabar += FormateoString(QuitoTildes(BPAPE?.Substring(45, Math.Min(15, BPAPE.Length - 45))), 15, ' ', true) + "&&";
                                                StringGrabar += FormateoString(QuitoTildes(BPAPE?.Substring(60, Math.Min(30, BPAPE.Length - 60))), 30, ' ', true) + "&&";
                                                */
                                                StringGrabar += FormateoString(QuitoTildes((BPAPE?.Length >= 15 ? BPAPE.Substring(0, 15) : BPAPE?.PadRight(15) ?? string.Empty.PadRight(15))), 15, ' ', true) + "&&";
                                                StringGrabar += FormateoString(QuitoTildes((BPAPE?.Length >= 30 ? BPAPE.Substring(15, 15) : BPAPE?.Substring(15)?.PadRight(15) ?? string.Empty.PadRight(15))), 15, ' ', true) + "&&";
                                                StringGrabar += FormateoString(QuitoTildes((BPAPE?.Length >= 45 ? BPAPE.Substring(30, 15) : BPAPE?.Substring(30)?.PadRight(15) ?? string.Empty.PadRight(15))), 15, ' ', true) + "&&";
                                                StringGrabar += FormateoString(QuitoTildes((BPAPE?.Length >= 60 ? BPAPE.Substring(45, 15) : BPAPE?.Substring(45)?.PadRight(15) ?? string.Empty.PadRight(15))), 15, ' ', true) + "&&";
                                                StringGrabar += FormateoString(QuitoTildes((BPAPE?.Length > 60 ? BPAPE.Substring(60).PadRight(30) : string.Empty.PadRight(30))), 30, ' ', true) + "&&";

                                            }
                                        }

                                        if (!string.IsNullOrEmpty(registro.IBANB))
                                        {
                                            StringGrabar += FormateoString(registro.IBANB, 28, ' ', true) + "&&";
                                        }
                                        else
                                        {
                                            StringGrabar += FormateoString(registro.TRFBCTA, 28, ' ', true) + "&&";
                                        }

                                        if (registro.TRFBBCO == 0)
                                        {
                                            StringGrabar += FormateoString("117", 5, ' ', true) + "&&";
                                        }
                                        else
                                        {
                                            StringGrabar += FormateoString(registro.TRFBBCO.ToString(), 5, ' ', true) + "&&";
                                        }

                                        StringGrabar += FormateoString(registro.TRFNUM, 20, ' ', true) + "&&";

                                        Pais = registro.TRFPAIS?.Trim();
                                        DeptoO = registro.TRFODEPT?.Trim();
                                        DeptoD = registro.TRFDDEPT?.Trim();

                                        if (Pais == "GT")
                                        {
                                            Pais = "US";
                                        }

                                        StringGrabar += FormateoString(Pais, 2, ' ', true) + "&&";

                                        if (Pais == "US")
                                        {
                                            if (string.IsNullOrEmpty(DeptoO))
                                            {
                                                DeptoO = "FL";
                                            }
                                            StringGrabar += FormateoString(DeptoO, 2, ' ', true) + "&&";
                                        }
                                        else
                                        {
                                            StringGrabar += FormateoString(" ", 2, ' ', true) + "&&";
                                        }

                                        StringGrabar += FormateoString("01", 2, ' ', true) + "&&";

                                        SucursalTrn = registro.TRFBRN?.Trim();
                                        if (string.IsNullOrEmpty(SucursalTrn))
                                        {
                                            SucursalTrn = "0";
                                        }

                                        if (int.Parse(SucursalTrn) > 100)
                                        {
                                            SucursalTrn = "0";
                                        }

                                        StringGrabar += FormateoString(SucursalTrn, 10, ' ', true) + "&&";
                                        StringGrabar += FormateoString(FormateoMontos(registro.TRFMNT.ToString()), 14, ' ', true) + "&&";

                                        if (registro.TRFCCY == "QTZ")
                                        {
                                            StringGrabar += "GTQ" + "&&";
                                        }
                                        else
                                        {
                                            StringGrabar += FormateoString(registro.TRFCCY, 3, ' ', true) + "&&";
                                        }

                                        StringGrabar += FormateoString(FormateoMontos(registro.TRFMNTD.ToString()), 14, ' ', true) + "&&";


                                        break; 
                                }
                                break;
                            case "4":
                                if (!string.IsNullOrEmpty(registro.TRFOCUN) && registro.TRFOCUN != "0")
                                {
                                    //var registrosTemporales = ConsultarIVETRF21Temporal(registro.TRFOCUN);}
                                    var registrosTemporales = listaIVE21TemporalGlobal.Where(x => x.Cliente == float.Parse(registro.TRFOCUN));
                                    if (registrosTemporales.Any())
                                    {
                                        StringGrabar = "";
                                        StringGrabar += registro.TRFFECHA.ToString("yyyyMMdd") + "&&";
                                        StringGrabar += "4" + "&&";
                                        StringGrabar += "E" + "&&";

                                        foreach (var registroTemporal in registrosTemporales)
                                        {
                                            if (registroTemporal.String.StartsWith("I"))
                                            {
                                                StringGrabar += FormateoString(registroTemporal.String.Trim(), 135, ' ', true) + "&&";
                                            }
                                            else
                                            {
                                                StringGrabar += FormateoString(registroTemporal.String.Trim(), 135, ' ', true) + "&&";
                                            }
                                        }
                                        CltOrd = true;
                                    }
                                    else
                                    {
                                        CltOrd = false;
                                    }
                                }
                                else
                                {
                                    CltOrd = false;
                                }

                                if (CltOrd == false)
                                {
                                    StringGrabar = "";
                                    StringGrabar += registro.TRFFECHA.ToString("yyyyMMdd") + "&&";
                                    StringGrabar += "4" + "&&";
                                    StringGrabar += "E" + "&&";
                                    OTIPOP = registro.TRFOTPER.Trim();                                    
                                    if (OTIPOP == "I")
                                    {
                                        OTIPOID = registro.TRFOTID.Trim();
                                        if (OTIPOID == "C")
                                        {
                                            OORDEN = registro.TRFOORD.Trim();
                                            OID = registro.TRFODOC.Trim();
                                            OMUNI = registro.TRFOMUN.Trim();
                                            OPAPE = registro.TRFOAPE1.Trim();
                                            OSAPE = registro.TRFOAPE2.Trim();
                                            OACAS = registro.TRFOAPEC.Trim();
                                            OPNOM = registro.TRFONOM1.Trim();
                                            OSNOM = registro.TRFONOM2.Trim();
                                        }else
                                        {
                                            OORDEN = registro.TRFOORD.Trim();
                                            OID = registro.TRFODOC.Trim();
                                            OMUNI = registro.TRFOMUN.Trim();
                                            OPAPE = registro.TRFOAPE1.Trim();
                                            OSAPE = registro.TRFOAPE2.Trim();
                                            OACAS = registro.TRFOAPEC.Trim();
                                            OPNOM = registro.TRFONOM1.Trim();
                                            OSNOM = registro.TRFONOM2.Trim();
                                        }
                                        StringGrabar += FormateoString(OTIPOP, 1, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OTIPOID, 1, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OORDEN, 3, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OID, 20, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OMUNI, 2, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OPAPE, 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OSAPE, 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OACAS, 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OPNOM, 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OSNOM, 30, ' ', true) + "&&";
                                    }                                    
                                                                    
                                    else
                                    {
                                        OTIPOID = registro.TRFOTID.Trim();
                                        OORDEN = "   ";
                                        OID = registro.TRFODOC.Trim();
                                        OMUNI = "  ";
                                        OPAPE = registro.TRFOAPE1.Trim();

                                        StringGrabar += FormateoString(OTIPOP, 1, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OTIPOID, 1, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OORDEN, 3, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OID, 20, ' ', true) + "&&";
                                        StringGrabar += FormateoString(OMUNI, 2, ' ', true) + "&&";
                                        /*
                                        StringGrabar += FormateoString(QuitoTildes(OPAPE.Substring(0, 15).Trim()), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes(OPAPE.Substring(15, 15).Trim()), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes(OPAPE.Substring(30, 15).Trim()), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes(OPAPE.Substring(45, 15).Trim()), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes(OPAPE.Substring(60, 30).Trim()), 30, ' ', true) + "&&";
                                        */
                                        StringGrabar += FormateoString(QuitoTildes((OPAPE?.Length >= 15 ? OPAPE.Substring(0, 15).Trim() : OPAPE?.PadRight(15) ?? string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes((OPAPE?.Length >= 30 ? OPAPE.Substring(15, 15).Trim() : OPAPE?.Substring(15)?.PadRight(15) ?? string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes((OPAPE?.Length >= 45 ? OPAPE.Substring(30, 15).Trim() : OPAPE?.Substring(30)?.PadRight(15) ?? string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes((OPAPE?.Length >= 60 ? OPAPE.Substring(45, 15).Trim() : OPAPE?.Substring(45)?.PadRight(15) ?? string.Empty.PadRight(15)).Substring(0, 15)), 15, ' ', true) + "&&";
                                        StringGrabar += FormateoString(QuitoTildes((OPAPE?.Length > 60 ? OPAPE.Substring(60).Trim().PadRight(30) : string.Empty.PadRight(30)).Substring(0, 30)), 30, ' ', true) + "&&";

                                    }
                                }

                                if (!string.IsNullOrEmpty(registro.IBANO))
                                {
                                    StringGrabar += FormateoString(registro.IBANO, 28, ' ', true) + "&&";
                                }
                                else
                                {
                                    StringGrabar += FormateoString(registro.TRFOCTA, 28, ' ', true) + "&&";
                                }

                                StringGrabar += " " + "&&";
                                StringGrabar += " " + "&&";
                                StringGrabar += "   " + "&&";
                                StringGrabar += new string(' ', 20) + "&&";
                                StringGrabar += "  " + "&&";

                                NombreTmp = "POR PAGO DE PLANILLAS";
                                /*
                                StringGrabar += FormateoString(QuitoTildes(NombreTmp.Substring(0, 15).Trim()), 15, ' ', true) + "&&";
                                StringGrabar += FormateoString(QuitoTildes(NombreTmp.Substring(15, 15).Trim()), 15, ' ', true) + "&&";
                                StringGrabar += FormateoString(QuitoTildes(NombreTmp.Substring(30, 15).Trim()), 15, ' ', true) + "&&";
                                StringGrabar += FormateoString(QuitoTildes(NombreTmp.Substring(45, 15).Trim()), 15, ' ', true) + "&&";
                                StringGrabar += FormateoString(QuitoTildes(NombreTmp.Substring(60, 30).Trim()), 30, ' ', true) + "&&";
                                */
                                
                                StringGrabar += FormateoString(QuitoTildes(NombreTmp != null && NombreTmp.Length > 0 ? NombreTmp.PadRight(15).Substring(0, 15).Trim() : string.Empty.PadRight(15)), 15, ' ', true) + "&&";
                                StringGrabar += FormateoString(QuitoTildes(NombreTmp != null && NombreTmp.Length > 15 ? NombreTmp.PadRight(30).Substring(15, 15).Trim() : string.Empty.PadRight(15)), 15, ' ', true) + "&&";
                                StringGrabar += FormateoString(QuitoTildes(NombreTmp != null && NombreTmp.Length > 30 ? NombreTmp.PadRight(45).Substring(30, 15).Trim() : string.Empty.PadRight(15)), 15, ' ', true) + "&&";
                                StringGrabar += FormateoString(QuitoTildes(NombreTmp != null && NombreTmp.Length > 45 ? NombreTmp.PadRight(60).Substring(45, 15).Trim() : string.Empty.PadRight(15)), 15, ' ', true) + "&&";
                                StringGrabar += FormateoString(QuitoTildes(NombreTmp != null && NombreTmp.Length > 60 ? NombreTmp.PadRight(90).Substring(60, 30).Trim() : string.Empty.PadRight(30)), 30, ' ', true) + "&&";


                                if (!string.IsNullOrEmpty(registro.IBANB))
                                {
                                    StringGrabar += FormateoString(registro.IBANB, 28, ' ', true) + "&&";
                                }
                                else
                                {
                                    StringGrabar += FormateoString(registro.TRFBCTA, 28, ' ', true) + "&&";
                                }

                                StringGrabar += FormateoString(" ", 5, ' ', true) + "&&";
                                StringGrabar += FormateoString(registro.TRFNUM, 20, ' ', true) + "&&";
                                StringGrabar += FormateoString(registro.TRFPAIS, 2, ' ', true) + "&&";
                                StringGrabar += FormateoString(registro.TRFODEPT, 2, ' ', true) + "&&";
                                StringGrabar += FormateoString(registro.TRFDDEPT, 2, ' ', true) + "&&";

                                SucursalTrn = registro.TRFBRN;
                                if (string.IsNullOrEmpty(SucursalTrn))
                                {
                                    SucursalTrn = "0";
                                }
                                if (double.TryParse(SucursalTrn, out double sucursalValue) && sucursalValue > 100)
                                {
                                    SucursalTrn = "0";
                                }

                                StringGrabar += FormateoString(SucursalTrn, 10, ' ', true) + "&&";
                                StringGrabar += FormateoString(FormateoMontos(registro.TRFMNT.ToString()), 14, ' ', true) + "&&";

                                if (registro.TRFCCY == "QTZ")
                                {
                                    StringGrabar += "GTQ" + "&&";
                                }
                                else
                                {
                                    StringGrabar += FormateoString(registro.TRFCCY, 3, ' ', true) + "&&";
                                }

                                StringGrabar += FormateoString(FormateoMontos(registro.TRFMNTD.ToString()), 14, ' ', true) + "&&";


                                break;

                        }
                        fileWriter.WriteLine(QuitoTildes(StringGrabar));
                        cantidadRegsDetalleOK++;
                    }                    

                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    File.Delete(filePath);
                    return fileBytes;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(" Error en TransaccionesClientes " + ex.Message);
            }            
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
        private List<DTO_IVETRF21Temporal> ConsultarIVETRF21Temporal()
        {
            List<DTO_IVETRF21Temporal> listaDatos = new List<DTO_IVETRF21Temporal>();

            string query = "SELECT * FROM IVE_TRF21_TEMPORAL";            

            try
            {
                DataTable dt = _dbHelper.ExecuteSelectCommand(query);
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
        private List<DTO_IVETRF21Archivos> ConsultarIVETRF21(int año, int mes)
        {
            List<DTO_IVETRF21Archivos> listaDatos = new List<DTO_IVETRF21Archivos>();            

            string query = @"
                SELECT * 
                FROM IVE_TRF21_Archivos 
                WHERE Mes = @Mes
                  AND Ano = @Ano";

            SqlParameter[] parameters = {
                        new SqlParameter("@Ano", año),                        
                        new SqlParameter("@Mes", mes)
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
                throw new Exception("Error en IVE_TRF21_Archivos: " + ex.Message);
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
                        Nombre = row["NombreCliente"].ToString(),
                        Tipo = int.Parse(row["TipoCliente"].ToString()),
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
                        CodCliente = int.Parse(row["cod_cliente"].ToString()),
                        CodClienteAnt = int.Parse(row["cod_cliente_ant"].ToString()),
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
                        CodCliente =  int.TryParse(row["cod_cliente"].ToString(), out var codCliente) ? codCliente : (int?)null,
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
        private List<DTO_IVE21TRF> ConsultarIVE21TRFPorFecha(int año, int mes)
        {
            List<DTO_IVE21TRF> listaDatos = new List<DTO_IVE21TRF>();

            //string queryReal = @"
            //    SELECT DISTINCT 
            //        IVE21Transferencia.*, 
            //        DWAGENCIA.*, 
            //        dwcuenta_iban.iban AS ibano, 
            //        dwcuenta_ibanben.iban AS ibanb
            //    FROM 
            //        IVE21Transferencia
            //    LEFT OUTER JOIN 
            //        DWAGENCIA ON AGENCIAID = TRFBRN
            //    LEFT OUTER JOIN 
            //        [172.16.4.62].dwbinter.dbo.dwcuenta_iban dwcuenta_iban ON dwcuenta_iban.cuenta COLLATE SQL_Latin1_General_CP1_CI_AS = trfocta
            //    LEFT OUTER JOIN 
            //        [172.16.4.62].dwbinter.dbo.dwcuenta_iban dwcuenta_ibanben ON dwcuenta_ibanben.cuenta COLLATE SQL_Latin1_General_CP1_CI_AS = trfbcta
            //    WHERE  
            //        ive21 = 'S' 
            //        AND YEAR(trffecha) = 2024  -- Reemplaza con el valor real de CmbAno.Text
            //        AND MONTH(trffecha) = 1    -- Reemplaza con (CmbMes.ListIndex + 1)
            //    ORDER BY 
            //        trffecha, trftipo, trftran;
            //";
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
                new SqlParameter("@Anio", año),
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
                        TRFFECHA = DateTime.Parse(row["TRFFECHA"].ToString()),
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
                        //IbanIdOrigen = row["ibanid"] as int?,
                        //CuentaOrigen = row["cuenta"] as long?,
                        IBANO = row["ibano"]?.ToString(),
                        //IbanFormatoOrigen = row["iban_formato"]?.ToString(),

                        // Propiedades de dwcuenta_ibanben
                        //IbanIdBeneficiario = row["ibanid"] as int?,
                        //CuentaBeneficiario = row["cuenta"] as long?,
                        IBANB = row["ibanb"]?.ToString(),
                        //IbanFormatoBeneficiario = row["iban_formato"]?.ToString(),

                        


                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error en ConsultarIVE21TRFPorFecha: " + ex.Message, ex);
            }

            return listaDatos;
        }

        // Método para consultar ubicaciones geográficas
        private List<DTO_UbicacionGeografica> ConsultarUbicacionesGeograficas()
        {
            List<DTO_UbicacionGeografica> listaDatos = new List<DTO_UbicacionGeografica>();

            string query = @"
                    SELECT 
                        ubicaciongeoid, 
                        nombrepais, 
                        nombredepartamento, 
                        nombremunicipio, 
                        paisid, 
                        departamentoid, 
                        municipioid, 
                        Cod_Orbe_Muni, 
                        Cod_orbe_pais, 
                        cod_SIB_Muni, 
                        MRPeso, 
                        cod_INE
                    FROM 
                        dwubicaciongeografica";

            try
            {
                DataTable dt = _dbHelper.ExecuteSelectCommand(query);

                foreach (DataRow row in dt.Rows)
                {
                    listaDatos.Add(new DTO_UbicacionGeografica
                    {
                        UbicacionGeoId = Convert.ToInt16(row["ubicaciongeoid"]),
                        NombrePais = row["nombrepais"].ToString(),
                        NombreDepartamento = row["nombredepartamento"].ToString(),
                        NombreMunicipio = row["nombremunicipio"].ToString(),
                        PaisId = Convert.ToInt16(row["paisid"]),
                        DepartamentoId = Convert.ToInt16(row["departamentoid"]),
                        MunicipioId = Convert.ToInt32(row["municipioid"]),
                        CodOrbeMuni = row["Cod_Orbe_Muni"].ToString(),
                        CodOrbePais = row["Cod_orbe_pais"].ToString(),
                        CodSibMuni = row["cod_SIB_Muni"].ToString(),
                        MRPeso = Convert.ToInt16(row["MRPeso"]),
                        CodINE = row["cod_INE"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error en ConsultarUbicacionesGeograficas: " + ex.Message);
            }

            return listaDatos;
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
