using IVEBA_API_Rest.Models.IVE17DV;

namespace IVEBA_API_Rest.Services.IVE17DV
{
    public interface IIVE17DVHelperService
    {
        public Task<List<DTO_IVE17DV>> ConsultarIVE17DV();
        public Task<List<DTO_IVE17DV>> ConsultarIVE17DVRangoFechas(int fechaInicial, int fechaFinal);
    }
}
