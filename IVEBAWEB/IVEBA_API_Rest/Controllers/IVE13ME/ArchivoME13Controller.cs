using IVEBA_API_Rest.Models.IVE13ME;
using IVEBA_API_Rest.Services.IVE13ME;
using IVEBA_API_Rest.Utilidades;
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
        public async Task<DTO_IVE13MEResponse> GenerarArchivoIVE13ME(int fechaInicial, int fechaFinal)
        {
            try
            {
                return await IVE13MEService.GeneracionArchivoIVE13ME(fechaInicial, fechaFinal);                
            }
            catch (Exception ex)
            {
                return null;
            }
        }        

    }
}
