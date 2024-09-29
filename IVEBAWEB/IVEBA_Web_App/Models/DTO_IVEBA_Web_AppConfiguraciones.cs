using Newtonsoft.Json;

namespace IVEBA_Web_App.Models
{
    public class DTO_IVEBA_Web_AppConfiguraciones
    {
        [JsonProperty("APIUrlBase")]
        public string APIUrlBase { get; set; }
    }
}
