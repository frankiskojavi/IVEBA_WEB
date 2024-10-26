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
        public async Task<List<DTO_IVE17DV>> ConsultarInformacionArchivoIVE17DV()
        {
            List<DTO_IVE17DV> listaRegistros = await IVE17DVService.ConsultarIVE17DV();
            return listaRegistros;
        }

        [HttpGet("[action]")]
        public async Task<List<DTO_IVE17DV>> ConsultarInformacionArchivoIVE17DVPorFecha(int fechaInicial, int fechaFinal)
        {
            List<DTO_IVE17DV> listaRegistros = await IVE17DVService.ConsultarIVE17DVRangoFechas(fechaInicial, fechaFinal);
            return listaRegistros;
        }
    }
}