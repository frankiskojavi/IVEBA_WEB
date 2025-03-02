using IVEBA_API_Rest.Models.IVEBA05;
using IVEBA_API_Rest.Services.IVEBA05;
using Microsoft.AspNetCore.Mvc;

namespace IVEBA_API_Rest.Controllers.IVE17DV
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArchivoBA05Controller : Controller
    {
        private readonly IIVEBA05HelperService IVEBA05Service;

        public ArchivoBA05Controller(IIVEBA05HelperService IVEBA05Service)
        {
            this.IVEBA05Service = IVEBA05Service;
        }

        [HttpGet("[action]")]
        public async Task<DTO_IVEBA05Response> GenerarArchivoIVEBA05(int fechaInicial, int fechaFinal)
        {
            try
            {
                int ano = fechaInicial / 10000; 
                int mes = (fechaInicial / 100) % 100;
                return await IVEBA05Service.GenerarArchivoIVEBA05(mes, ano);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}