using IVEBA_API_Rest.Helpers;
using IVEBA_API_Rest.Models.IVE13ME;
using System.Data;
using System.Data.SqlClient;

namespace IVEBA_API_Rest.Services.IVE13ME
{
    public class IVE13MEHelperService : IIVE13MEHelperService
    {
        private readonly DbHelper _dbHelper;
        public IVE13MEHelperService(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }
        public async Task<List<DTO_IVE13ME>> ConsultarIVE13ME()
        {
            List<DTO_IVE13ME> empleados = new List<DTO_IVE13ME>();
            string query = "SELECT * FROM IVE13ME";
            DataTable dt = _dbHelper.ExecuteSelectCommand(query);

            foreach (DataRow row in dt.Rows)
            {
                empleados.Add(new DTO_IVE13ME
                {
                    LineaId = row["LineaId"].ToString(),
                    Fecha = int.Parse(row["Fecha"].ToString()),
                    Transaccion = row["Transaccion"].ToString(),
                    Tipo_Moneda = row["Tipo_Moneda"].ToString(),
                    MontoMO = decimal.Parse(row["MontoMO"].ToString()),
                    MontoUSD = decimal.Parse(row["MontoUSD"].ToString()),
                    Cantidad_Trx = int.Parse(row["Cantidad_Trx"].ToString()),
                    Agenciaid = int.Parse(row["Agenciaid"].ToString()),
                    ORDEN = int.Parse(row["ORDEN"].ToString()),
                });
            }

            return empleados;
        }

        public async Task<List<DTO_IVE13ME>> ConsultarIVE13MEPorRangoFechas(int fechaInicial, int fechaFinal)
        {
            List<DTO_IVE13ME> empleados = new List<DTO_IVE13ME>();
            string query = "SELECT * FROM IVE13ME WHERE Fecha between @FechaInicial and @FechaFinal ORDER BY ORDEN";
            SqlParameter[] parameters = {
                new SqlParameter("@FechaInicial", fechaInicial),
                new SqlParameter("@FechaFinal", fechaFinal)
            };
            DataTable dt = _dbHelper.ExecuteSelectCommand(query, parameters);

            foreach (DataRow row in dt.Rows)
            {
                empleados.Add(new DTO_IVE13ME
                {
                    LineaId = row["LineaId"].ToString(),
                    Fecha = int.Parse(row["Fecha"].ToString()),
                    Transaccion = row["Transaccion"].ToString(),
                    Tipo_Moneda = row["Tipo_Moneda"].ToString(),
                    MontoMO = decimal.Parse(row["MontoMO"].ToString()),
                    MontoUSD = decimal.Parse(row["MontoUSD"].ToString()),
                    Cantidad_Trx = int.Parse(row["Cantidad_Trx"].ToString()),
                    Agenciaid = int.Parse(row["Agenciaid"].ToString()),
                    ORDEN = int.Parse(row["ORDEN"].ToString()),
                });
            }

            return empleados;
        }
    }
}
