using IVEBA_API_Rest.Models.IVE13ME;

namespace IVEBA_API_Rest.Services.IVE13ME
{
    public interface IIVE13MEHelperService
    {
        public Task<List<DTO_IVE13ME>> ConsultarIVE13ME();
        public Task<List<DTO_IVE13ME>> ConsultarIVE13MEPorRangoFechas(int fechaInicial, int fechaFinal);
    }
}
