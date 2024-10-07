using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IVEBA_API_Rest.Models.IVECH;

namespace IVEBA_API_Rest.Services.IVECH
{
    public interface IIVECHHelperService
    {
        public Task<int> EliminaCHCajaTemporal();
        public Task<List<DTO_IVECHClientesCaja>> ConsultarClientesCHCajaTemporal(int fechaInicial, int fechaFinal);
    }
}