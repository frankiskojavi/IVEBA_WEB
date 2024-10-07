using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using IVEBA_API_Rest.Helpers;
using IVEBA_API_Rest.Models.IVECH;

namespace IVEBA_API_Rest.Services.IVECH
{
    public class IVECHHelperService: IIVECHHelperService
    {
        private readonly DbHelper _dbHelper;
        private readonly IConfiguration _configuration;
        public IVECHHelperService(DbHelper dbHelper, IConfiguration configuration)
        {
            _dbHelper = dbHelper;
            _configuration = configuration;
        }

        public async Task<int> EliminaCHCajaTemporal()
        {
            string query = "DELETE FROM IVE_CH_CAJA_Temporal";
            int filasAfectadas = 0;
            try
            {
                filasAfectadas = _dbHelper.ExecuteNonQuery(query);
            }
            catch (Exception ex)
            {
                throw new Exception ("Error al eliminar IVE_CH_CAJA_Temporal " + ex.Message);
            }

            return filasAfectadas;
        }

        public async Task <List<DTO_IVECHClientesCaja>> ConsultarClientesCHCajaTemporal(int fechaInicial, int fechaFinal){
            List<DTO_IVECHClientesCaja> resultado = new List<DTO_IVECHClientesCaja>();
            string query = $"Select   Distinct Clt as Cliente,   NombreCliente as Nombre,   TipoCliente as Tipo from   VChCaja   inner join DWCLIENTE on CLT = COD_CLIENTE where   Fec >= {fechaInicial}   and Fec <= {fechaFinal}   and Clt <> '0'   and Cheq <> 0";

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
    }
}