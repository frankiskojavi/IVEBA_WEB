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
        public async Task<List<DTO_IVE14EF>> ConsultarInformacionArchivoIVE14EF()
        {
            List<DTO_IVE14EF> listaRegistros = await IVE14EFService.ConsultarIVE14EF();
            return listaRegistros;
        }

        [HttpGet("[action]")]
        public async Task<List<DTO_IVE14EF>> ConsultarInformacionArchivoIVE14EFPorFecha(int fechaInicial, int fechaFinal)
        {
            List<DTO_IVE14EF> listaRegistros = await IVE14EFService.ConsultarIVE14EFPorRangoFechas(fechaInicial, fechaFinal);
            return listaRegistros;
        }
    }
}
