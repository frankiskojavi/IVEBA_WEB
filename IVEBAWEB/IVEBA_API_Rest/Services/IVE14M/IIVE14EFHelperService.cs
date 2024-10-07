using IVEBA_API_Rest.Models.IVE14EF;

namespace IVEBA_API_Rest.Services.IVE14EF
{
    public interface IIVE14EFHelperService
    {
        public Task<List<DTO_IVE14EF>> ConsultarIVE14EF();
        public Task<List<DTO_IVE14EF>> ConsultarIVE14EFPorRangoFechas(int fechaInicial, int fechaFinal);
    }
}
