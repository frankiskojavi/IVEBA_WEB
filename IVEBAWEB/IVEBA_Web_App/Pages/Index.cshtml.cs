using IVEBA_Web_App.Models;
using IVEBA_Web_App.Models.SeguridadAPP;
using IVEBA_Web_App.Services.SeguridadAPP;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IVEBA_Web_App.Pages
{
    public class IndexModel : PageModel
    {
        private readonly iSeguridadaAPPService service;
        public List<DTO_OpcionesAPPResponse> formData;
        public IndexModel (iSeguridadaAPPService service){ 
            this.service = service;
        }
        public async Task OnGetAsync()
        {            
            try
            {
                formData = await service.ConsultarOpcionesMenuWebApp();
            }
            catch (Exception ex)
            {
                RedirectToPage("/Error");
            }
         
        }

        public async Task OnPost()
        {
            try
            {
                formData = await service.ConsultarOpcionesMenuWebApp();
            }
            catch (Exception ex)
            {
                RedirectToPage("/Error");
            }

        }
    }
}
