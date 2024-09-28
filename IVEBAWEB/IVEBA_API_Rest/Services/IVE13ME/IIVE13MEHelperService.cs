using IVEBA_API_Rest.Models.DTOS;

namespace IVEBA_API_Rest.Services.IVE13ME
{
    public interface IIVE13MEHelperService
    {
        public IEnumerable<DTO_IVE13ME> ConsultarIVE13ME();
        public IEnumerable<DTO_IVE13ME> ConsultarIVE13MEPorRangoFechas(int fechaInicial, int fechaFinal);
    }
}
