using IVEBA_Web_App.Models.ArchivoME13;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IVEBA_Web_App.Services.ArchivoME13
{
    public interface iGeneracionArchivoME13
    {
        public Task<DTO_ME13_Form> cargarInformacionPorDefecto();
        public Task<List<SelectListItem>> recuperarMesesComboBox();
        public Task<List<SelectListItem>> recuperarAñosComboBox();
        public Task<List<DTO_IVE13ME_Response>> ConsultarInformacionArchivoIVE13MEPorFecha(int fechaInicial, int fechaFinal);
    }
}
