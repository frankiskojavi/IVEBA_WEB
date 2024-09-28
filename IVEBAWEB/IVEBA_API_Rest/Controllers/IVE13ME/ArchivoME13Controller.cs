using IVEBA_API_Rest.Models.DTOS;
using IVEBA_API_Rest.Services.IVE13ME;
using Microsoft.AspNetCore.Mvc;

namespace IVEBA_API_Rest.Controllers.IVE13ME
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArchivoME13Controller : Controller
    {
        private readonly IIVE13MEHelperService IVE13MEService;
        public ArchivoME13Controller(IIVE13MEHelperService IVE13MEService)
        {
            this.IVE13MEService = IVE13MEService;
        }

        [HttpGet("[action]")]
        public IEnumerable<DTO_IVE13ME> ConsultarInformacionArchivoIVE13ME()
        {
            return IVE13MEService.ConsultarIVE13ME();
        }

        [HttpGet("[action]")]
        public IEnumerable<DTO_IVE13ME> ConsultarInformacionArchivoIVE13MEPorFecha(int fechaInicial, int fechaFinal)
        {
            return IVE13MEService.ConsultarIVE13MEPorRangoFechas(fechaInicial, fechaFinal);
        }
    }
}
