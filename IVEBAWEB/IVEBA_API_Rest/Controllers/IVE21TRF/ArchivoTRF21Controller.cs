using IVEBA_API_Rest.Models.IVE17DV;
using IVEBA_API_Rest.Services.IVE21TRF;
using Microsoft.AspNetCore.Mvc;

namespace IVEBA_API_Rest.Controllers.IVE21TRF
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArchivoTRF21Controller : Controller
    {
        private readonly IIVE21TRFHelperService IVE21trfService;

        public ArchivoTRF21Controller(IIVE21TRFHelperService IVE21trfService)
        {
            this.IVE21trfService = IVE21trfService;
        }

        [HttpGet("[action]")]
        public async Task<DTO_IVE21TRFResponse> GenerarArchivoIVE21TRF(int fechaInicial, int fechaFinal)
        {
            try
            {
                return await IVE21trfService.GeneracionArchivoIVE21TRF(fechaInicial, fechaFinal, true);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}