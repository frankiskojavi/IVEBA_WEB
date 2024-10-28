using IVEBA_API_Rest.Models.IVE13ME;
using IVEBA_API_Rest.Models.IVE14EF;
using IVEBA_API_Rest.Services.IVE14EF;
using Microsoft.AspNetCore.Mvc;

namespace IVEBA_API_Rest.Controllers.IVE14EF
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArchivoEF14Controller : Controller
    {
        private readonly IIVE14EFHelperService IVE14EFService;

        public ArchivoEF14Controller(IIVE14EFHelperService IVE14EFService)
        {
            this.IVE14EFService = IVE14EFService;
        }

        [HttpGet("[action]")]
        public async Task<DTO_IVE14EFResponse> GenerarArchivoIVE14EF(int fechaInicial, int fechaFinal)
        {
            try
            {
                return await IVE14EFService.GeneracionArchivoIVE14EF(fechaInicial, fechaFinal);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
