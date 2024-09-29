using Microsoft.AspNetCore.Mvc.TagHelpers;

namespace IVEBA_Web_App.Models.ArchivoME13
{
    public class DTO_ME13_Form
    {
        public string codigoArchivo { get; set; }        
        public int  año { get; set; }
        public int mes { get; set; }
        public string nombreArchivo { get; set; }
        public int registrosProcesados{ get; set;}
        public int registrosConError{get; set;}
        public int detalle { get; set; }
        public string nit { get; set; }
        public int generaError { get; set; }
    }
}
 