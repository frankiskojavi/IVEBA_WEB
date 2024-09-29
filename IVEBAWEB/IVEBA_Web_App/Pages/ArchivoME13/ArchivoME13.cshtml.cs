using IVEBA_Web_App.Models.ArchivoME13;
using IVEBA_Web_App.Services.ArchivoME13;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IVEBA_Web_App.Pages.ArchivoME13
{
    public class ArchivoME13Model : PageModel
    {
        [BindProperty]
        public DTO_ME13_Form FormModel { get; set; }
        public List<SelectListItem> Meses { get; set; }
        public List<SelectListItem> Años { get; set; }

        private readonly iGeneracionArchivoME13 Service;

        public ArchivoME13Model(iGeneracionArchivoME13 service)
        {
            Service = service;
        }
        public IActionResult OnGet()
        {
            try
            {
                cargarInformacionDefault();
            }
            catch (Exception ex)
            {
                return RedirectToPage("/Error");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostGenerarArchivoME13Async()
        {
            try
            {
                return await generarInformacionArchivoME13();
                // Respuesta al Cliente

            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    errorMessage = "Ocurrió un error al generar el archivo: " + ex.Message
                });
            }
        }

        public async void cargarInformacionDefault()
        {
            Meses = await Service.recuperarMesesComboBox();
            Años = await Service.recuperarAñosComboBox();
            FormModel = await Service.cargarInformacionPorDefecto();
        }

        public async Task<JsonResult> generarInformacionArchivoME13()
        {
            // Contenido del archivo de texto
            string contendoArchivo = "Hola Mundo";
            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(contendoArchivo);
            string fileBase64 = Convert.ToBase64String(fileBytes);
            string fileName = FormModel.nombreArchivo;
            int fechaInicial = 20240801;
            int fechaFinal = 20240830;
            List<DTO_IVE13ME_Response> data = await Service.ConsultarInformacionArchivoIVE13MEPorFecha(fechaInicial, fechaFinal);

            return new JsonResult(new
            {
                success = true,
                registrosProcesados = data.Count(),
                registrosConError = 0,
                fileName = fileName,
                fileContent = fileBase64
            });

        }


    }
}
