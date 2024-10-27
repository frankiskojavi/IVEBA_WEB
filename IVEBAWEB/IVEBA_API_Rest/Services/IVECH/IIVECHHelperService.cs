namespace IVEBA_API_Rest.Services.IVECH
{
    public interface IIVECHHelperService
    {
        public Task<bool> GeneracionTemporalIVECH(int fechaInicial, int fechaFinal);
    }
}