using IVEBA_API_Rest.Models.IVE14EF;
using IVEBA_API_Rest.Models.IVE17DV;

namespace IVEBA_API_Rest.Services.IVE17DV
{
    public interface IIVE17DVHelperService
    {        
        public Task<DTO_IVE17DVResponse> GeneracionArchivoIVE17DV(int fechaInicial, int fechaFinal, bool archivoDefinitivo);
    }
}
