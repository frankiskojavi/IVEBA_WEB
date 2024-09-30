using IVEBA_Web_App.Models;
using IVEBA_Web_App.Models.ArchivoME13;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System.Net.Http.Headers;
using System.Text;

namespace IVEBA_Web_App.Services.ArchivoME13
{
    public class GeneracionArchivoME13 : iGeneracionArchivoME13
    {
        private readonly DTO_IVEBA_Web_AppConfiguraciones configuraciones;
        private List<SelectListItem> Meses { get; set; }
        private List<SelectListItem> Años { get; set; }

        private string APIUrlBase;

        public GeneracionArchivoME13(IOptions<DTO_IVEBA_Web_AppConfiguraciones> settings)
        {
            configuraciones = settings.Value;
            APIUrlBase = configuraciones.APIUrlBase;
        }
        public async Task<List<SelectListItem>> recuperarMesesComboBox()
        {
            Meses = new List<SelectListItem>
            {
                new SelectListItem("Enero", "1"),
                new SelectListItem("Febrero", "2"),
                new SelectListItem("Marzo", "3"),
                new SelectListItem("Abril", "4"),
                new SelectListItem("Mayo", "5"),
                new SelectListItem("Junio", "6"),
                new SelectListItem("Julio", "7"),
                new SelectListItem("Agosto", "8"),
                new SelectListItem("Septiembre", "9"),
                new SelectListItem("Octubre", "10"),
                new SelectListItem("Noviembre", "11"),
                new SelectListItem("Diciembre", "12")
            };
            return Meses;
        }

        public async Task<List<SelectListItem>> recuperarAñosComboBox()
        {
            int currentYear = DateTime.Now.Year;
            Años = new List<SelectListItem>();
            for (int year = 2005; year <= currentYear; year++)
            {
                Años.Add(new SelectListItem(year.ToString(), year.ToString()));
            }
            return Años;
        }

        public async Task<DTO_ME13_Form> cargarInformacionPorDefecto()
        {
            DTO_ME13_Form? archivoME13 = null;
            try
            {
                string codigoArchivo = "IVEME13";
                int añoSistema = System.DateTime.Now.Year;
                int mesSistema = System.DateTime.Now.Month;

                archivoME13 = new DTO_ME13_Form
                {
                    codigoArchivo = codigoArchivo,
                    año = añoSistema,
                    mes = mesSistema,
                    nombreArchivo = $"{codigoArchivo}{añoSistema}{mesSistema.ToString("00")}BA.117",
                    registrosProcesados = 0,
                    registrosConError = 0,
                    detalle = 0,
                    nit = "",
                    generaError = 0
                };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return archivoME13;
        }

        public async Task<List<DTO_IVE13ME_Response>> ConsultarInformacionArchivoIVE13MEPorFecha(int año, int mes)
        {

            DateTime fechaInicialDate = new DateTime(año, mes, 1);
            DateTime fechaFinalDate = fechaInicialDate.AddMonths(1).AddDays(-1);

            // Convertir las fechas al formato YYYYMMDD como enteros
            int fechaInicial = int.Parse(fechaInicialDate.ToString("yyyyMMdd"));
            int fechaFinal = int.Parse(fechaFinalDate.ToString("yyyyMMdd"));


            string APIUrl = $"{APIUrlBase}/ArchivoME13/ConsultarInformacionArchivoIVE13MEPorFecha?fechaInicial={fechaInicial}&fechaFinal={fechaFinal}";
            string jwtToken = "tu_token_jwt_aqui";
            List<DTO_IVE13ME_Response>? apiResponses = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                    HttpResponseMessage response = await client.GetAsync(APIUrl);

                    string ResponseBody = await response.Content.ReadAsStringAsync();
                    apiResponses = JsonConvert.DeserializeObject<List<DTO_IVE13ME_Response>>(ResponseBody);


                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        Log.Error("Ocurrio un error al consultar api ConsultarInformacionArchivoIVE13MEPorFecha : " + response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Ocurrio un error al consultar api ConsultarInformacionArchivoIVE13MEPorFecha : " + ex.Message);
                throw new Exception("Error al consultar Información IVE13ME");
            }

            return apiResponses;
        }

        public async Task<byte[]> GenerarArchivoIVE13ME(List<DTO_IVE13ME_Response> data)
        {
            StringBuilder contenidoArchivo = new StringBuilder();
            byte[] fileBytes = null;
            try {
                // Generar las líneas basadas en cada item de la lista `data`
                foreach (var item in data)
                {

                    string StringImprimir = FormateoString(item.LineaId.ToString(), 16) + "&&" +
                                            item.Fecha + "&&" +
                                            item.Transaccion + "&&" +
                                            item.Tipo_Moneda + "&&" +
                                            FormateoString(FormateoMontos(item.MontoMO), 15, " ", true) + "&&" +
                                            FormateoString(FormateoMontos(item.MontoUSD), 15, " ", true) + "&&" +
                                            FormateoString(item.Cantidad_Trx.ToString(), 10, " ", true) + "&&" +
                                            FormateoString(item.Agenciaid.ToString(), 10, " ", true);


                    contenidoArchivo.AppendLine(StringImprimir);
                }
                fileBytes = System.Text.Encoding.UTF8.GetBytes(contenidoArchivo.ToString());
            }catch(Exception ex) {
                Log.Error("Ocurrio un error en GenerarArchivoIVE13ME: " + ex.Message);
                throw new Exception("Error en la generación del archivo IVE13ME");
            }    

            return fileBytes;
        }

        public string FormateoString(string input, int totalWidth, string paddingChar = " ", bool padLeft = false)
        {
            if (padLeft)
                return input.PadLeft(totalWidth, paddingChar[0]);
            else
                return input.PadRight(totalWidth, paddingChar[0]);
        }

        public string FormateoMontos(decimal monto)
        {

            return monto.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);  // Formato con 2 decimales sin comas        
        }

    }
}
