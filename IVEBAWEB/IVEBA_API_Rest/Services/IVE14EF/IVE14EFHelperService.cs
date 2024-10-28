using IVEBA_API_Rest.Helpers;
using IVEBA_API_Rest.Models.IVE13ME;
using IVEBA_API_Rest.Models.IVE14EF;
using IVEBA_API_Rest.Utilidades;
using Microsoft.Win32;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace IVEBA_API_Rest.Services.IVE14EF
{
    public class IVE14EFHelperService : IIVE14EFHelperService
    {
        private readonly DbHelper _dbHelper;
        private readonly UtilidadesAPP utilidades;
        public IVE14EFHelperService(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
            utilidades = new UtilidadesAPP();
        }
        public async Task<DTO_IVE14EFResponse> GeneracionArchivoIVE14EF(int fechaInicial, int fechaFinal)
        {
            int cantidadRegistrosOK = 0;
            DTO_IVE14EFResponse respuesta = new DTO_IVE14EFResponse();

            try
            {
                List<DTO_IVE14EF> registros = ConsultarIVE14EFPorRangoFechas(fechaInicial, fechaFinal);

                // Definir el nombre del archivo temporal
                string filePath = Path.Combine(Path.GetTempPath(), "archivoGenerado.txt");

                if (registros.Count > 0)
                {
                    using (StreamWriter archivo = new StreamWriter(filePath))
                    {
                        foreach (var registro in registros)
                        {
                            string stringImprimir = utilidades.FormateoString(registro.LineaId, 16, " ", "SI") + "&&" +
                                                    registro.Fecha + "&&" +
                                                    registro.Transaccion + "&&" +
                                                    utilidades.FormateoString(utilidades.FormateoMontos(registro.Monto.ToString()), 15, " ") + "&&" +
                                                    utilidades.FormateoString(registro.Cantidad_Trx.ToString(), 10, " ") + "&&" +
                                                    utilidades.FormateoString(registro.Agenciaid.ToString(), 10, " ");

                            archivo.WriteLine(stringImprimir);
                            cantidadRegistrosOK++;
                        }
                    }

                    // Leer el archivo generado como arreglo de bytes
                    byte[] fileBytes = File.ReadAllBytes(filePath);

                    // Eliminar el archivo temporal
                    File.Delete(filePath);

                    respuesta.cantidadRegistrosOK = cantidadRegistrosOK;
                    respuesta.cantidadRegistrosError = 0;
                    respuesta.archivoTXT = fileBytes;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error en GeneracionArchivoIVE14EF : " + ex.Message);
            }
            return respuesta;
        }


        private List<DTO_IVE14EF> ConsultarIVE14EFPorRangoFechas(int fechaInicial, int fechaFinal)
        {
            List<DTO_IVE14EF> listaDatos = new List<DTO_IVE14EF>();
            string query = "SELECT * FROM IVE14EF WHERE Fecha between @FechaInicial and @FechaFinal ORDER BY ORDEN";
            try
            {
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
            }
            catch (Exception ex)
            {
                throw new Exception("Error en ConsultarIVE14EFPorRangoFechas : " + ex.Message);
            }

            return listaDatos;
        }
    }
}
