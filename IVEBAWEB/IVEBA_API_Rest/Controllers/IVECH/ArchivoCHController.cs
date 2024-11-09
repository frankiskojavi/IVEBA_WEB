using IVEBA_API_Rest.Models.IVECH;
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

        [HttpGet("GenerarArchivoDefinitivo")]
        public async Task<ActionResult<DTO_CHCajaTemporalResponse>> GenerarArchivoDefinitivo(int fechaInicial, int fechaFinal)
        {
            var response = await IVECHService.GenerarArchivoCH(true, fechaInicial, fechaFinal);
            return Ok(response);
        }

        [HttpGet("GenerarArchivoTemporal")]
        public async Task<ActionResult<DTO_CHCajaTemporalResponse>> GenerarArchivoTemporal(int fechaInicial, int fechaFinal)
        {
            var response = await IVECHService.GenerarArchivoCH(false, fechaInicial, fechaFinal);
            return Ok(response);
        }


    }
}