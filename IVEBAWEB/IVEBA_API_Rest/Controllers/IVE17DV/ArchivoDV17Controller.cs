using IVEBA_API_Rest.Models.IVE14EF;
using IVEBA_API_Rest.Models.IVE17DV;
using IVEBA_API_Rest.Services.IVE17DV;
using Microsoft.AspNetCore.Mvc;

namespace IVEBA_API_Rest.Controllers.IVE17DV
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArchivoDV17Controller : Controller
    {
        private readonly IIVE17DVHelperService IVE17DVService;

        public ArchivoDV17Controller(IIVE17DVHelperService IVE17DVService)
        {
            this.IVE17DVService = IVE17DVService;
        }

        [HttpGet("[action]")]
        public async Task<DTO_IVE17DVResponse> GenerarArchivoIVE17DV(int fechaInicial, int fechaFinal)
        {
            try
            {
                return await IVE17DVService.GeneracionArchivoIVE17DV(fechaInicial, fechaFinal, true);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}