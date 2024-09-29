using IVEBA_Web_App.Models;
using IVEBA_Web_App.Models.ArchivoME13;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System.Net.Http.Headers;

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
                    nombreArchivo = $"C:/{codigoArchivo}/{añoSistema}{mesSistema.ToString("00")}BA.117",
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

        public async Task<List<DTO_IVE13ME_Response>> ConsultarInformacionArchivoIVE13MEPorFecha(int fechaInicial, int fechaFinal)
        {
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
            }

            return apiResponses;
        }

    }
}
