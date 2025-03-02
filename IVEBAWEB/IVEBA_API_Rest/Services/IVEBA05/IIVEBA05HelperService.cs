using IVEBA_API_Rest.Models.IVE14EF;
using IVEBA_API_Rest.Models.IVE17DV;
using IVEBA_API_Rest.Models.IVEBA05;

namespace IVEBA_API_Rest.Services.IVEBA05
{
    public interface IIVEBA05HelperService
    {        
        public Task<DTO_IVEBA05Response> GenerarArchivoIVEBA05(int mes, int año);
    }
}
