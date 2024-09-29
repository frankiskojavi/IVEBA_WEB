using IVEBA_Web_App.Models.ArchivoME13;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IVEBA_Web_App.Pages.ArchivoME13
{
    public class ArchivoME13Model : PageModel
    {
        [BindProperty]
        public ArchivoME13DTO FormModel { get; set; }
        public List<SelectListItem> Meses { get; set; }
        public List<SelectListItem> Años { get; set; }

        public void OnGet()
        {
            // Inicializar valores al cargar el formulario
            cargarInformacionDefault();
        }

        /*
        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            return RedirectToPage("/Index");
        }
        */

        public IActionResult OnPostGenerarArchivoME13()
        {            
            // Simulación de proceso
            FormModel.registrosProcesados = 100; // Ejemplo de valores modificados
            FormModel.registrosConError = 5;

            // Retornar un resultado JSON con los datos procesados
            return new JsonResult(new
            {
                success = true,
                registrosProcesados = FormModel.registrosProcesados,
                registrosConError = FormModel.registrosConError
            });
        }

        public void cargarInformacionDefault(){ 
            string codigoArchivo = "IVEME13";
            int añoSistema = System.DateTime.Now.Year;
            int mesSistema = System.DateTime.Now.Month;

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
            int currentYear = DateTime.Now.Year;
            Años = new List<SelectListItem>();
            for (int year = 2005; year <= currentYear; year++)
            {
                Años.Add(new SelectListItem(year.ToString(), year.ToString()));
            }

            FormModel = new ArchivoME13DTO
            {
                codigoArchivo = codigoArchivo,
                año = añoSistema,
                mes = mesSistema,
                nombreArchivo = $"C:/{ codigoArchivo }/{ añoSistema }{ mesSistema.ToString("00") }BA.117",
                registrosProcesados = 0, 
                registrosConError = 0,
                detalle = 0,
                nit = "",
                generaError = 0
            };
        }
    }
}
