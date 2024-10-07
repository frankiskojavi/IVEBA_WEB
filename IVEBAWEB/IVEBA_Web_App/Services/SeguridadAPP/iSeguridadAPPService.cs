using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IVEBA_Web_App.Models.SeguridadAPP;

namespace IVEBA_Web_App.Services.SeguridadAPP
{
    public interface iSeguridadaAPPService
    {        
        public Task<List<DTO_OpcionesAPPResponse>> ConsultarOpcionesMenuWebApp();

    }
}