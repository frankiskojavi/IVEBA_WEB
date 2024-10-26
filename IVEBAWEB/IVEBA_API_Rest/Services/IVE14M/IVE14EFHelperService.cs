using IVEBA_API_Rest.Helpers;
using IVEBA_API_Rest.Models.IVE14EF;
using System.Data;
using System.Data.SqlClient;

namespace IVEBA_API_Rest.Services.IVE14EF
{
    public class IVE14EFHelperService : IIVE14EFHelperService
    {
        private readonly DbHelper _dbHelper;
        public IVE14EFHelperService(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }
        public async Task<List<DTO_IVE14EF>> ConsultarIVE14EF()
        {
            List<DTO_IVE14EF> listaDatos = new List<DTO_IVE14EF>();
            string query = "SELECT * FROM IVE14EF";
            DataTable dt = _dbHelper.ExecuteSelectCommand(query);

            foreach (DataRow row in dt.Rows)
            {
                listaDatos.Add(new DTO_IVE14EF
                {
                    LineaId = row["LineaId"].ToString(),
                    Fecha = int.Parse(row["Fecha"].ToString()),
                    Transaccion = row["Transaccion"].ToString(),
                    Monto = decimal.Parse(row["Monto"].ToString()),
                    Cantidad_Trx = int.Parse(row["Cantidad_Trx"].ToString()),
                    Agenciaid = int.Parse(row["Agenciaid"].ToString()),
                    ORDEN = int.Parse(row["ORDEN"].ToString()),
                });
            }

            return listaDatos;
        }

        public async Task<List<DTO_IVE14EF>> ConsultarIVE14EFPorRangoFechas(int fechaInicial, int fechaFinal)
        {
            List<DTO_IVE14EF> listaDatos = new List<DTO_IVE14EF>();
            string query = "SELECT * FROM IVE14EF WHERE Fecha between @FechaInicial and @FechaFinal ORDER BY ORDEN";
            SqlParameter[] parameters = {
                new SqlParameter("@FechaInicial", fechaInicial),
                new SqlParameter("@FechaFinal", fechaFinal)
            };
            DataTable dt = _dbHelper.ExecuteSelectCommand(query, parameters);

            foreach (DataRow row in dt.Rows)
            {
                listaDatos.Add(new DTO_IVE14EF
                {
                    LineaId = row["LineaId"].ToString(),
                    Fecha = int.Parse(row["Fecha"].ToString()),
                    Transaccion = row["Transaccion"].ToString(),
                    Monto = decimal.Parse(row["Monto"].ToString()),
                    Cantidad_Trx = int.Parse(row["Cantidad_Trx"].ToString()),
                    Agenciaid = int.Parse(row["Agenciaid"].ToString()),
                    ORDEN = int.Parse(row["ORDEN"].ToString()),
                });
            }

            return listaDatos;
        }
    }
}
