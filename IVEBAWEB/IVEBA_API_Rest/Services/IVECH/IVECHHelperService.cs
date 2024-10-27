using IVEBA_API_Rest.Helpers;
using IVEBA_API_Rest.Models.IVECH;
using System.Data;

namespace IVEBA_API_Rest.Services.IVECH
{
    public class IVECHHelperService : IIVECHHelperService
    {
        private readonly DbHelper _dbHelper;
        private readonly IConfiguration _configuration;

        public int contadorErrores;
        public string textoErrores;
        public IVECHHelperService(DbHelper dbHelper, IConfiguration configuration)
        {
            _dbHelper = dbHelper;
            _configuration = configuration;
        }
        public async Task<bool> GeneracionTemporalIVECH(int fechaInicial, int fechaFinal)
        {
            string registroError = "";
            string ErrorGeneral = "";
            try
            {
                // Elimina archivo temporal
                EliminaCHCajaTemporal();

                // Recupera clientes CH Caja                
                List<DTO_IVECHClientesCaja> listaClientes = ConsultarClientesCHCajaTemporal(fechaInicial, fechaFinal);
                foreach (DTO_IVECHClientesCaja cliente in listaClientes)
                {
                    int codigoCliente = cliente.Cliente;
                    int tipoCliente = cliente.Tipo;
                    string nombreCliente = cliente.Nombre.Replace("'", " ");
                    string datosPersona = "";
                    string datosEmpresa = "";

                    switch (tipoCliente)
                    {
                        case 2:
                        case 3:
                            if (ProcesoFisicos(codigoCliente, ref datosPersona))
                            {
                                // OK
                                InsertarCHCajaTemporal(codigoCliente, datosPersona.Trim(), 0, 0, 0);
                            }
                            else
                            {
                                // Error
                                InsertarCHCajaTemporal(codigoCliente, "ERROR", 99, 99, 9999);
                                registroError = codigoCliente.ToString("D12") + " " + nombreCliente + " " + ErrorGeneral + "/n";
                                contadorErrores++;
                                textoErrores = "";
                            }
                            break;

                        case 4:
                        case 1:

                            if (ProcesoJuridicos(codigoCliente, ref datosPersona))
                            {
                                // OK
                                InsertarCHCajaTemporal(codigoCliente, datosPersona.Trim(), 0, 0, 0);
                            }
                            else
                            {
                                // Error
                                InsertarCHCajaTemporal(codigoCliente, "ERROR", 99, 99, 9999);
                                registroError = codigoCliente.ToString("D12") + " " + nombreCliente + " " + ErrorGeneral + "/n";
                                contadorErrores++;
                                textoErrores = "";
                            }
                            break;
                        default:
                            registroError = codigoCliente.ToString("D12") + " " + nombreCliente + " sin tipo. /n";
                            break;
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Error en  GeneracionTemporalIVECH " + ex.Message);
            }
            return true;
        }
        private bool ProcesoFisicos(int codigoCliente, ref string stringDatos)
        {
            bool variableFisico = false;
            string stringArmado = "";

            // Obtener el cliente usando el método existente
            DTO_DWCliente clienteDW = ConsultarDWCliente(codigoCliente);

            try
            {
                if (clienteDW != null)
                {
                    stringArmado = "I";
                    string orden = "";

                    switch (clienteDW.TipoIdentificacion)
                    {
                        case 1: // Cedula
                            orden = clienteDW.Identificacion.Substring(0, 3);
                            if (orden[1] == '0')
                            {
                                orden = orden[0].ToString() + orden[2].ToString();
                            }
                            orden = FormateoString(orden, 3, " ", 1);
                            stringArmado += "C" + orden;
                            stringArmado += FormateoString(Convert.ToInt32(clienteDW.Identificacion.Substring(4, 7)).ToString(), 20, " ", 1);
                            break;

                        case 2: // Partida
                            orden = FormateoString("", 3, " ", 1);
                            stringArmado += "O" + orden;
                            stringArmado += FormateoString(clienteDW.Identificacion, 20, " ", 1);
                            break;

                        case 4: // Pasaporte
                            orden = FormateoString("", 3, " ", 1);
                            stringArmado += "P" + orden;
                            stringArmado += FormateoString(clienteDW.Identificacion, 20, " ", 1);
                            break;

                        case 26: // DPI
                            orden = "   ";
                            stringArmado += "D" + orden;
                            stringArmado += FormateoString(clienteDW.Identificacion, 20, " ", 1);
                            break;
                    }

                    stringArmado += FormateoString(QuitoTildes(clienteDW.Apellido1.ToUpper()), 15, " ", 1);
                    stringArmado += FormateoString(QuitoTildes(clienteDW.Apellido2.ToUpper()), 15, " ", 1);
                    stringArmado += FormateoString(QuitoTildes(clienteDW.ApellidoCasada.ToUpper()), 15, " ", 1);
                    stringArmado += FormateoString(QuitoTildes(clienteDW.Nombre1.ToUpper()), 15, " ", 1);
                    stringArmado += FormateoString(QuitoTildes(clienteDW.Nombre2.ToUpper()), 15, " ", 1);

                    variableFisico = true;
                    stringDatos = stringArmado;
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return variableFisico;
        }
        private bool ProcesoJuridicos(int cliente, ref string stringDatos)
        {
            bool variableEmpresa = false;
            string stringArmado = "";

            // Consultar cliente usando el método existente
            DTO_DWCliente varEmpresa = ConsultarDWCliente(cliente);

            if (varEmpresa != null)
            {
                string nitEmpresa;

                // Validación de NIT
                if (string.IsNullOrEmpty(varEmpresa.Nit) || varEmpresa.Nit == "0")
                {
                    nitEmpresa = "";
                }
                else
                {
                    nitEmpresa = varEmpresa.Nit;
                }

                // Si NIT está vacío o es 0, establecer "SINNIT" o la identificación
                if (string.IsNullOrEmpty(nitEmpresa))
                {
                    if (varEmpresa.TipoIdentificacion == 8)
                    {
                        nitEmpresa = varEmpresa.Identificacion.Trim();
                    }
                    else
                    {
                        nitEmpresa = "SINNIT";
                        //txtNit.Text = (int.Parse(txtNit.Text) + 1).ToString();
                    }

                    if (nitEmpresa == "0" || string.IsNullOrEmpty(nitEmpresa))
                        nitEmpresa = "SINNIT";
                }

                // Ajuste para el cliente con código 10
                if (cliente == 10)
                    nitEmpresa = "1205544";

                // Construcción de la cadena final
                stringArmado = "J";
                stringArmado += "N";
                stringArmado += "   ";
                stringArmado += FormateoString(QuitoCaracter(nitEmpresa.Trim()), 20, " ", 1);
                stringArmado += FormateoString(QuitoTildes(varEmpresa.NombreCliente), 75, " ", 1);

                variableEmpresa = true;
                stringDatos = stringArmado;
            }

            return variableEmpresa;
        }

        private string FormateoString(string stringAFormatear, int digitos, string relleno, int orientacion = 1)
        {
            int registro = stringAFormatear.Trim().Length;
            if (registro <= digitos)
            {
                if (orientacion == 1)
                    return stringAFormatear.Trim() + new string(relleno[0], digitos - registro);
                else
                    return new string(relleno[0], digitos - registro) + stringAFormatear.Trim();
            }
            else
            {
                return stringAFormatear.Trim().Substring(0, digitos);
            }
        }

        private string FormateoMontos(string montoATransformar)
        {
            int monto = (int)(Convert.ToDouble(montoATransformar) * 100);
            return monto.ToString();
        }

        private string QuitoTildes(string stringQuitar)
        {
            stringQuitar = stringQuitar.Replace("Á", "A")
                                       .Replace("É", "E")
                                       .Replace("Í", "I")
                                       .Replace("Ó", "O")
                                       .Replace("Ú", "U")
                                       .Replace("-", " ")
                                       .Replace("/", " ")
                                       .Replace("$", " ")
                                       .Replace("&", " ");
            return stringQuitar;
        }

        private string QuitoCaracter(string stringQuitar)
        {
            string stringTemporal = stringQuitar;

            if (stringQuitar.Contains("-") || stringQuitar.Contains(","))
            {
                stringTemporal = stringTemporal.Replace(",", "").Replace("-", "");
            }

            return stringTemporal;
        }


        private int InsertarCHCajaTemporal(int cliente, string cadena, int dia, int mes, int año)
        {
            string query = $"INSERT into IVE_CH_CAJA_TEMPORAL Values ({cliente}, '{cadena}', {dia}, {mes}, {año} );";
            int filasAfectadas = 0;
            try
            {
                filasAfectadas = _dbHelper.ExecuteNonQuery(query);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al insertar IVE_CH_CAJA_Temporal " + ex.Message);
            }

            return filasAfectadas;
        }

        private int EliminaCHCajaTemporal()
        {
            string query = "DELETE FROM IVE_CH_CAJA_Temporal";
            int filasAfectadas = 0;
            try
            {
                filasAfectadas = _dbHelper.ExecuteNonQuery(query);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al eliminar IVE_CH_CAJA_Temporal " + ex.Message);
            }

            return filasAfectadas;
        }

        private List<DTO_IVECHClientesCaja> ConsultarClientesCHCajaTemporal(int fechaInicial, int fechaFinal)
        {
            List<DTO_IVECHClientesCaja> resultado = new List<DTO_IVECHClientesCaja>();
            string query = $" Select " +
                           $"   Distinct Clt as Cliente, " +
                           $"   NombreCliente as Nombre, " +
                           $"   TipoCliente as Tipo " +
                           $" from " +
                           $"   VChCaja  " +
                           $"   inner join DWCLIENTE on CLT = COD_CLIENTE " +
                           $" where " +
                           $"   (Fec between {fechaInicial} and {fechaFinal})" +
                           $" and Clt <> '0' " +
                           $" and Cheq <> 0 ";

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
            return resultado;
        }

        private DTO_DWCliente ConsultarDWCliente(int codigoCliente)
        {
            DTO_DWCliente resultado = new DTO_DWCliente();
            string query = $" Select * from dwcliente where cod_cliente = {codigoCliente} ";

            DataTable dt = _dbHelper.ExecuteSelectCommand(query);

            foreach (DataRow row in dt.Rows)
            {
                resultado = new DTO_DWCliente
                {
                    CodCliente = long.Parse(row["cod_cliente"].ToString()),
                    CodClienteAnt = long.Parse(row["cod_cliente_ant"].ToString()),
                    NombreCliente = row["nombrecliente"].ToString(),
                    Identificacion = row["identificacion"].ToString(),
                    TipoIdentificacion = byte.Parse(row["tipoidentificacion"].ToString()),
                    IdentUbicacion = short.Parse(row["ident_ubicacion"].ToString()),
                    FNacimiento = int.Parse(row["fnacimiento"].ToString()),
                    TipoCliente = byte.Parse(row["tipocliente"].ToString()),
                    OficialCuenta = short.Parse(row["oficialcuenta"].ToString()),
                    Banca = byte.Parse(row["banca"].ToString()),
                    EstadoCivil = byte.Parse(row["estadocivil"].ToString()),
                    Genero = byte.Parse(row["genero"].ToString()),
                    Edad = byte.Parse(row["edad"].ToString()),
                    RangoEdad = byte.Parse(row["rangoedad"].ToString()),
                    ActividadEconomica = short.Parse(row["actividadeconomica"].ToString()),
                    FechaAgregado = int.Parse(row["fecha_agregado"].ToString()),
                    FechaModificado = int.Parse(row["fecha_modificado"].ToString()),
                    GrupoEconomico = short.Parse(row["grupoeconomico"].ToString()),
                    Profesion = short.Parse(row["profesion"].ToString()),
                    Email = row["email"].ToString(),
                    Nit = row["nit"].ToString(),
                    PaisCliente = short.Parse(row["pais_cliente"].ToString()),
                    Telefono1 = row["telefono1"].ToString(),
                    Telefono2 = row["telefono2"].ToString(),
                    Celular = row["celular"].ToString(),
                    Fax = row["fax"].ToString(),
                    Nombre1 = row["nombre1"].ToString(),
                    Nombre2 = row["nombre2"].ToString(),
                    Apellido1 = row["apellido1"].ToString(),
                    Apellido2 = row["apellido2"].ToString(),
                    ApellidoCasada = row["apellidocasada"].ToString(),
                    IngresoMensual = decimal.Parse(row["ingresomensual"].ToString()),
                    RelacionDependencia = bool.Parse(row["relacion_dependencia"].ToString()),
                    LugarTrabajo = row["lugar_trabajo"].ToString(),
                    CargoTrabajo = row["cargo_trabajo"].ToString(),
                    ViviendaPropia = row["vivienda_propia"].ToString(),
                    Bloqueo = byte.Parse(row["bloqueo"].ToString()),
                    FultActualizacion = int.Parse(row["fultactualizacion"].ToString()),
                    Cotitularidad = row["cotitularidad"].ToString(),
                    AgenciaApertura = short.Parse(row["agenciaapertura"].ToString()),
                    CalificacionRiesgo = byte.Parse(row["calificacionriesgo"].ToString()),
                    CategoriaRiesgo = row["categoriariesgo"].ToString(),
                    NombreConyuge = row["nombre_conyuge"].ToString(),
                    NumHijos = byte.Parse(row["num_hijos"].ToString()),
                    EnFormacion = row["en_formacion"].ToString(),
                    IntermFinanciera = byte.Parse(row["interm_financiera"].ToString()),
                    NombreUsual = row["nombreusual"].ToString(),
                    FrecOperaciones = byte.Parse(row["frec_operaciones"].ToString()),
                    RefExternas = byte.Parse(row["ref_externas"].ToString()),
                    Fuente = byte.Parse(row["fuente"].ToString()),
                    Comentarios = row["comentarios"].ToString(),
                    FolioLibro = row["Folio_Libro"].ToString(),
                    FechaEscritura = int.Parse(row["FechaEscritura"].ToString()),
                    Direccion = row["direccion"].ToString(),
                    DirPais = short.Parse(row["dir_pais"].ToString()),
                    DirDepto = short.Parse(row["dir_depto"].ToString()),
                    DirMunicpio = int.Parse(row["dir_municpio"].ToString()),
                    Zona = byte.Parse(row["zona"].ToString()),
                    Colonia = row["colonia"].ToString(),
                    CodigoPostal = row["codigopostal"].ToString(),
                    RetImp = byte.Parse(row["ret_imp"].ToString()),
                    ConocimientoAct = byte.Parse(row["conocimiento_act"].ToString()),
                    Documentacion = byte.Parse(row["documentacion"].ToString()),
                    UbicacionNegocio = byte.Parse(row["ubicacionnegocio"].ToString()),
                    Categoria = byte.Parse(row["categoria"].ToString()),
                    Indicador = byte.Parse(row["indicador"].ToString()),
                    IdentSociedad = row["ident_sociedad"].ToString(),
                    IdentEmpresa = row["ident_empresa"].ToString(),
                    RangosQ = row["rangos_Q"].ToString(),
                    RangosD = row["rangos_D"].ToString(),
                    Email2 = row["email2"].ToString(),
                    Pep = char.Parse(row["Pep"].ToString()),
                    NombreParientePEP = row["NombreParientePEP"].ToString(),
                    Parentesco = row["Parentesco"].ToString(),
                    LugarTrabajoParientePEP = row["Lugar_TrabajoParientePEP"].ToString(),
                    CargoParientePEP = row["Cargo_ParientePEP"].ToString(),
                    EsFamiliarPEP = char.Parse(row["EsFamiliarPEP"].ToString()),
                    FormUltAviso = int.Parse(row["form_ultaviso"].ToString()),
                    FormNumAvisos = short.Parse(row["form_numavisos"].ToString()),
                    FormImpresion = int.Parse(row["form_impresion"].ToString()),
                    FormAgImpresion = short.Parse(row["form_agimpresion"].ToString()),
                    EstadoCl = byte.Parse(row["estadocl"].ToString()),
                    Sector = byte.Parse(row["Sector"].ToString()),
                    SubSector = byte.Parse(row["SubSector"].ToString()),
                    PosConsolidada = byte.Parse(row["PosConsolidada"].ToString()),
                    FichaSectorial = row["FichaSectorial"].ToString(),
                    GeneradorME = byte.Parse(row["GeneradorME"].ToString()),
                    DescActividadEco = row["descactividadeco"].ToString(),
                    Expediente = int.Parse(row["expediente"].ToString()),
                    CatRiesgoBanguat = row["catriesgobanguat"].ToString(),
                    MRTipoPersona = short.Parse(row["MRTipoPersona"].ToString()),
                    MRActividadEco = short.Parse(row["MRActividadEco"].ToString()),
                    MRProfesion = short.Parse(row["MRProfesion"].ToString()),
                    MRPaisOrigen = short.Parse(row["MRPaisOrigen"].ToString()),
                    MRAgencia = short.Parse(row["MRAgencia"].ToString()),
                    MRIngresos = short.Parse(row["MRIngresos"].ToString()),
                    MRCategoria = short.Parse(row["MRCategoria"].ToString()),
                    MRExpediente = short.Parse(row["MRExpediente"].ToString()),
                    MRReferencias = short.Parse(row["MRReferencias"].ToString()),
                    MRRangoIngresos = short.Parse(row["MRRangoIngresos"].ToString()),
                    SubCategoria = short.Parse(row["SubCategoria"].ToString()),
                    MRAntiguedad = short.Parse(row["MRAntiguedad"].ToString()),
                    MRCantCtas = short.Parse(row["MRCantCtas"].ToString()),
                    NegocioPropio = char.Parse(row["NegocioPropio"].ToString()),
                    PEPUltAct = int.Parse(row["PEPUltAct"].ToString()),
                    PEPCond = row["PEPCond"].ToString(),
                    PEPPais = short.Parse(row["PEPPais"].ToString()),
                    PEPFAgregado = int.Parse(row["PEPFAgregado"].ToString()),
                    EsAsociadoPep = char.Parse(row["EsAsociadoPep"].ToString()),
                    NombreAsociadoPep = row["NombreAsociadoPep"].ToString(),
                    EmpMayor = char.Parse(row["EmpMayor"].ToString()),
                    EsCliente = byte.Parse(row["EsCliente"].ToString()),
                    MRIngresosMonto = decimal.Parse(row["MRIngresosMonto"].ToString()),
                    GrupoAfinidadId = short.Parse(row["GrupoAfinidadId"].ToString()),
                    FultMov = int.Parse(row["fultmov"].ToString()),
                    MrActMasExpuesta = short.Parse(row["MrActMasExpuesta"].ToString()),
                    MRProducto = short.Parse(row["MRProducto"].ToString()),
                    MrComparacionIngresos = short.Parse(row["MrComparacionIngresos"].ToString()),
                    MrCalificacion = byte.Parse(row["MrCalificacion"].ToString()),
                    VisitaFecha = int.Parse(row["visita_fecha"].ToString()),
                    VisitaRespuesta = row["visita_respuesta"].ToString(),
                    VisitaComentario = row["visita_comentario"].ToString(),
                    RevisionFecha = int.Parse(row["revision_fecha"].ToString()),
                    RevisionRespuesta = row["revision_respuesta"].ToString(),
                    RevisionComentario = row["revision_comentario"].ToString(),
                    UsrCalifica = row["usr_califica"].ToString(),
                    Actualizar = byte.Parse(row["Actualizar"].ToString()),
                    FAlertaAct = int.Parse(row["fAlertaAct"].ToString())
                };
            }
            return resultado;
        }

    }
}