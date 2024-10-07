using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using IVEBA_API_Rest.Models.IVECH;
using IVEBA_API_Rest.Services.IVECH;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
        public async Task<int> EliminaCHCajaTemporal()
        {
            return await IVECHService.EliminaCHCajaTemporal();            
        }

        [HttpGet("[action]")]
        public async Task<List<DTO_IVECHClientesCaja>> ConsultarClientesCHCajaTemporal(int fechaInicial, int fechaFinal)
        {
            return await IVECHService.ConsultarClientesCHCajaTemporal(fechaInicial, fechaFinal);
        }
    }
}