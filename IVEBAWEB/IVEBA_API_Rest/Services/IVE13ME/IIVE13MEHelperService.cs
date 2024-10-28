using IVEBA_API_Rest.Models.IVE13ME;

namespace IVEBA_API_Rest.Services.IVE13ME
{
    public interface IIVE13MEHelperService
    {
        public Task<DTO_IVE13MEResponse> GeneracionArchivoIVE13ME(int fechaInicial, int fechaFinal);
    }
}
