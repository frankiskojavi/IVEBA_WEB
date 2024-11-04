using IVEBA_API_Rest.Helpers;
using IVEBA_API_Rest.Models.IVECH;
using Microsoft.Win32;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IVEBA_API_Rest.Services.IVECH
{
    public class IVECHHelperService : IIVECHHelperService
    {
        private readonly DbHelper _dbHelper;
        private readonly IConfiguration _configuration;        
        

        public IVECHHelperService(DbHelper dbHelper, IConfiguration configuration)
        {
            _dbHelper = dbHelper;
            _configuration = configuration;
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