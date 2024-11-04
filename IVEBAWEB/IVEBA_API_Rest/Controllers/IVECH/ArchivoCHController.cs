using IVEBA_API_Rest.Services.IVECH;
using Microsoft.AspNetCore.Mvc;

namespace IVEBA_API_Rest.Controllers.IVECH
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArchivoCHController : Controller
    {
        private readonly IIVECHHelperService IVECHService;
        public ArchivoCHController(IIVECHHelperService IVECHService)
        {
            this.IVECHService = IVECHService;
        }

        [HttpGet("[action]")]
        public async Task<bool> EliminaCHCajaTemporal(int fechaInicial, int fechaFinal)
        {
            //return await IVECHService.GeneracionTemporalIVECH(fechaInicial, fechaFinal);
            return true;
        }
    }
}