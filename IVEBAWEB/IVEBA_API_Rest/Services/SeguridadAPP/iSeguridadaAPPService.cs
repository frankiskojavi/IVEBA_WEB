using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IVEBA_API_Rest.Models.DTOS;

namespace IVEBA_API_Rest.Services.SeguridadAPP
{
    public interface iSeguridadaAPPService
    {
        public Task<List<DTO_OpcionesAPP>> ConsultarOpcionesMenuWebApp(); 

    }
}