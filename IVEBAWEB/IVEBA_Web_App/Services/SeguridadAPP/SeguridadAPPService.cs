using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IVEBA_Web_App.Models;
using IVEBA_Web_App.Models.SeguridadAPP;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;

namespace IVEBA_Web_App.Services.SeguridadAPP
{
    public class SeguridadAPPService : iSeguridadaAPPService
    {
        private readonly DTO_IVEBA_Web_AppConfiguraciones configuraciones;
        private string APIUrlBase;
        public SeguridadAPPService(IOptions<DTO_IVEBA_Web_AppConfiguraciones> settings)
        {
            configuraciones = settings.Value;
            APIUrlBase = configuraciones.APIUrlBase;
        }
        public async Task<List<DTO_OpcionesAPPResponse>> ConsultarOpcionesMenuWebApp()
        {
            string APIUrl = $"{APIUrlBase}/SeguridadAPP/ConsultarOpcionesMenuWebApp";
            string jwtToken = "tu_token_jwt_aqui";
            List<DTO_OpcionesAPPResponse>? apiResponses = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                    HttpResponseMessage response = await client.GetAsync(APIUrl);

                    string ResponseBody = await response.Content.ReadAsStringAsync();
                    apiResponses = JsonConvert.DeserializeObject<List<DTO_OpcionesAPPResponse>>(ResponseBody);


                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        Log.Error("Ocurrio un error al consultar api ConsultarOpcionesMenuWebApp : " + response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Ocurrio un error al consultar api ConsultarOpcionesMenuWebApp : " + ex.Message);
                throw new Exception("Error al consultar Información Opciones Menu");
            }

            return apiResponses;
        }
    }
}