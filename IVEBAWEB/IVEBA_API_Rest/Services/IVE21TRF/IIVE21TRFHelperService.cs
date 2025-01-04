using IVEBA_API_Rest.Models.IVE17DV;

namespace IVEBA_API_Rest.Services.IVE21TRF
{
    public interface IIVE21TRFHelperService
    {        
        public Task<DTO_IVE17DVResponse> GeneracionArchivoIVE21TRF(int fechaInicial, int fechaFinal, bool archivoDefinitivo);
    }
}
