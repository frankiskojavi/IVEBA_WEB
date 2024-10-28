using IVEBA_API_Rest.Helpers;
using IVEBA_API_Rest.Models.IVE13ME;
using IVEBA_API_Rest.Utilidades;
using Microsoft.Win32;
using System.Data;
using System.Data.SqlClient;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IVEBA_API_Rest.Services.IVE13ME
{
    public class IVE13MEHelperService : IIVE13MEHelperService
    {
        private readonly DbHelper _dbHelper;
        private readonly UtilidadesAPP utilidades;
        public IVE13MEHelperService(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
            utilidades = new UtilidadesAPP();
        }          
        public async Task<DTO_IVE13MEResponse> GeneracionArchivoIVE13ME(int fechaInicial, int fechaFinal)
        {
            int cantidadRegistrosOK = 0;
            DTO_IVE13MEResponse respuesta = new DTO_IVE13MEResponse();
            try
            {
                List<DTO_IVE13ME> registros = ConsultarIVE13MEPorRangoFechas(fechaInicial, fechaFinal);

                // Definir el nombre del archivo temporal
                string filePath = Path.Combine(Path.GetTempPath(), "archivoGenerado.txt");

                if (registros.Count > 0)
                {
                    using (StreamWriter archivo = new StreamWriter(filePath))
                    {
                        foreach (var registro in registros)
                        {
                            string moneda = registro.Tipo_Moneda switch
                            {
                                "QTZ" => "GTQ",
                                "USD" => "USD",
                                "EUR" => "EUR",
                                _ => "USD"
                            };

                            string montoOriginal = utilidades.FormateoMontos(registro.MontoMO.ToString());
                            string montoUSD = utilidades.FormateoMontos(registro.MontoUSD.ToString());

                            string stringImprimir = utilidades.FormateoString(registro.LineaId, 16, " ", "SI") + "&&" + registro.Fecha + "&&" +
                                                    registro.Transaccion + "&&" + moneda + "&&" +
                                                    utilidades.FormateoString(montoOriginal, 15, " ") + "&&" +
                                                    utilidades.FormateoString(montoUSD, 15, " ") + "&&" +
                                                    utilidades.FormateoString(registro.Cantidad_Trx.ToString(), 10, " ") + "&&" +
                                                    utilidades.FormateoString(registro.Agenciaid.ToString(), 10, " ");

                            archivo.WriteLine(stringImprimir);
                            cantidadRegistrosOK++;
                        }
                    }

                    // Leer y retornar el archivo como arreglo de bytes
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
                throw new Exception("Error en GeneracionArchivoIVE13ME : " + ex.Message);                
            }
            return respuesta;
        }               
        private List<DTO_IVE13ME> ConsultarIVE13MEPorRangoFechas(int fechaInicial, int fechaFinal)
        {
            List<DTO_IVE13ME> listaDatos = new List<DTO_IVE13ME>();
            try
            {
                string query = "SELECT * FROM IVE13ME WHERE Fecha between @FechaInicial and @FechaFinal ORDER BY ORDEN";
                SqlParameter[] parameters = {
                new SqlParameter("@FechaInicial", fechaInicial),
                new SqlParameter("@FechaFinal", fechaFinal)
            };
                DataTable dt = _dbHelper.ExecuteSelectCommand(query, parameters);

                foreach (DataRow row in dt.Rows)
                {
                    listaDatos.Add(new DTO_IVE13ME
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

                return listaDatos;
            }
            catch (Exception ex)
            {
                throw new Exception("Error en ConsultarIVE13MEPorRangoFechas : " + ex.Message);
            }
        }
    }
}
