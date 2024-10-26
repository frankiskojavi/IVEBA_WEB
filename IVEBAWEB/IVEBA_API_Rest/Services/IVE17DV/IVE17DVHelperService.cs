using IVEBA_API_Rest.Helpers;
using IVEBA_API_Rest.Models.IVE17DV;
using System.Data;
using System.Data.SqlClient;

namespace IVEBA_API_Rest.Services.IVE17DV
{
    public class IVE17DVHelperService : IIVE17DVHelperService
    {
        private readonly DbHelper _dbHelper;
        public IVE17DVHelperService(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public async Task<List<DTO_IVE17DV>> ConsultarIVE17DV()
        {
            List<DTO_IVE17DV> listaDatos = new List<DTO_IVE17DV>();
            string query = "SELECT * FROM IVE17";
            DataTable dt = _dbHelper.ExecuteSelectCommand(query);

            foreach (DataRow row in dt.Rows)
            {
                listaDatos.Add(new DTO_IVE17DV
                {
                    Fecha = row["Fecha"].ToString(),
                    TipoTransaccion = row["TipoTransaccion"].ToString(),
                    TipoPersona = row["TipoPersona"].ToString(),
                    TipoIdentificacion = row["TipoIdentificacion"].ToString(),
                    NoOrden = row["NoOrden"].ToString(),
                    NoIdentificacion = row["NoIdentificacion"].ToString(),
                    MuniEmiCedula = row["MuniEmiCedula"].ToString(),
                    Apellido1 = row["Apellido1"].ToString(),
                    Apellido2 = row["Apellido2"].ToString(),
                    ApellidoCasada = row["ApellidoCasada"].ToString(),
                    Nombre1 = row["Nombre1"].ToString(),
                    Nombre2 = row["Nombre2"].ToString(),
                    NombreEmpresa = row["NombreEmpresa"].ToString(),
                    FNacimiento = row["FNacimiento"].ToString(),
                    PaisPersona = row["PaisPersona"].ToString(),
                    ActividadEco = row["ActividadEco"].ToString(),
                    Detalle = row["Detalle"].ToString(),
                    Zona = row["Zona"].ToString(),
                    Depto = row["Depto"].ToString(),
                    Municipio = row["Municipio"].ToString(),
                    OrigenFondos = row["OrigenFondos"].ToString(),
                    TipoMoneda = row["TipoMoneda"].ToString(),
                    MontoOriginal = row["MontoOriginal"].ToString(),
                    MontoD = row["MontoD"].ToString(),
                    Agencia = row["Agencia"].ToString(),
                    Usuario = row["Usuario"].ToString(),
                    Asiento = row["Asiento"].ToString(),
                    Documento = row["Documento"].ToString(),
                    CodCliente = long.Parse(row["COD_CLIENTE"].ToString()),
                    NombreCliente = row["NOMBRECLIENTE"].ToString(),
                    DescripcionTrn = row["DESCRIPCIONTRN"].ToString()
                });
            }

            return listaDatos;
        }

        public async Task<List<DTO_IVE17DV>> ConsultarIVE17DVRangoFechas(int fechaInicial, int fechaFinal)
        {
            List<DTO_IVE17DV> listaDatos = new List<DTO_IVE17DV>();
            string query = "SELECT * FROM IVE17 WHERE Fecha between @FechaInicial and @FechaFinal";
            SqlParameter[] parameters = {
                new SqlParameter("@FechaInicial", fechaInicial),
                new SqlParameter("@FechaFinal", fechaFinal)
            };
            DataTable dt = _dbHelper.ExecuteSelectCommand(query, parameters);

            foreach (DataRow row in dt.Rows)
            {
                listaDatos.Add(new DTO_IVE17DV
                {
                    Fecha = row["Fecha"].ToString(),
                    TipoTransaccion = row["TipoTransaccion"].ToString(),
                    TipoPersona = row["TipoPersona"].ToString(),
                    TipoIdentificacion = row["TipoIdentificacion"].ToString(),
                    NoOrden = row["NoOrden"].ToString(),
                    NoIdentificacion = row["NoIdentificacion"].ToString(),
                    MuniEmiCedula = row["MuniEmiCedula"].ToString(),
                    Apellido1 = row["Apellido1"].ToString(),
                    Apellido2 = row["Apellido2"].ToString(),
                    ApellidoCasada = row["ApellidoCasada"].ToString(),
                    Nombre1 = row["Nombre1"].ToString(),
                    Nombre2 = row["Nombre2"].ToString(),
                    NombreEmpresa = row["NombreEmpresa"].ToString(),
                    FNacimiento = row["FNacimiento"].ToString(),
                    PaisPersona = row["PaisPersona"].ToString(),
                    ActividadEco = row["ActividadEco"].ToString(),
                    Detalle = row["Detalle"].ToString(),
                    Zona = row["Zona"].ToString(),
                    Depto = row["Depto"].ToString(),
                    Municipio = row["Municipio"].ToString(),
                    OrigenFondos = row["OrigenFondos"].ToString(),
                    TipoMoneda = row["TipoMoneda"].ToString(),
                    MontoOriginal = row["MontoOriginal"].ToString(),
                    MontoD = row["MontoD"].ToString(),
                    Agencia = row["Agencia"].ToString(),
                    Usuario = row["Usuario"].ToString(),
                    Asiento = row["Asiento"].ToString(),
                    Documento = row["Documento"].ToString(),
                    CodCliente = long.Parse(row["COD_CLIENTE"].ToString()),
                    NombreCliente = row["NOMBRECLIENTE"].ToString(),
                    DescripcionTrn = row["DESCRIPCIONTRN"].ToString()
                });
            }

            return listaDatos;
        }
    }
}
