using IVEBA_API_Rest.Helpers;
using IVEBA_API_Rest.Models.DTOS;
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
        public IEnumerable<DTO_IVE13ME> ConsultarIVE13ME()
        {
            var empleados = new List<DTO_IVE13ME>();
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

        public IEnumerable<DTO_IVE13ME> ConsultarIVE13MEPorRangoFechas(int fechaInicial, int fechaFinal)
        {
            var empleados = new List<DTO_IVE13ME>();
            string query = "SELECT * FROM IVE13ME WHERE Fecha between @FechaInicial and @FechaFinal";
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
