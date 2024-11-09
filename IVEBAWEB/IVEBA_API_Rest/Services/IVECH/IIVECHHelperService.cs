using IVEBA_API_Rest.Models.IVECH;

namespace IVEBA_API_Rest.Services.IVECH
{
    public interface IIVECHHelperService
    {
        public Task<DTO_CHCajaTemporalResponse> GenerarArchivoCH(bool archivoDefinitivo, int fechaInicial, int fechaFinal);
    }
}