using IVEBA_Web_App.Models.ArchivoME13;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IVEBA_Web_App.Pages.ArchivoME13
{
    public class ArchivoME13Model : PageModel
    {
        [BindProperty]
        public ArchivoME13DTO FormModel { get; set; }

        public void OnGet()
        {
            // Inicializar valores al cargar el formulario
            FormModel = new ArchivoME13DTO
            {
                Codigo = 100,   // Valor inicial para Codigo
                Nombre = "Ejemplo Nombre"  // Valor inicial para Nombre
            };
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            return RedirectToPage("/Index");
        }
    }
}
