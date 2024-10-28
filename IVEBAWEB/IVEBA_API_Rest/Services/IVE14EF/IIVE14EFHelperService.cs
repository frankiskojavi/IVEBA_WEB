using IVEBA_API_Rest.Models.IVE13ME;
using IVEBA_API_Rest.Models.IVE14EF;

namespace IVEBA_API_Rest.Services.IVE14EF
{
    public interface IIVE14EFHelperService
    {
        public Task<DTO_IVE14EFResponse> GeneracionArchivoIVE14EF (int fechaInicial, int fechaFinal);
    }
}
