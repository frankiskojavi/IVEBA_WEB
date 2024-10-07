using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IVEBA_API_Rest.Models.DTOS;
using IVEBA_API_Rest.Services.SeguridadAPP;
using Microsoft.AspNetCore.Mvc;

namespace IVEBA_API_Rest.Controllers.SeguridadAPP
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeguridadAPPController : Controller
    {
        private readonly iSeguridadaAPPService seguridadAPP; 
        
        public SeguridadAPPController(iSeguridadaAPPService seguridadAPP)
        { 
            this.seguridadAPP = seguridadAPP;
        }

        [HttpGet("[action]")]
        public async Task<List<DTO_OpcionesAPP>> ConsultarOpcionesMenuWebApp()
        {
            List<DTO_OpcionesAPP> listaRegistros = await seguridadAPP.ConsultarOpcionesMenuWebApp();
            return listaRegistros;
        }
    }
}