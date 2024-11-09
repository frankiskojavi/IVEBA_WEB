namespace IVEBA_API_Rest.Models.IVECH
{
    public class DTO_CHCajaTemporalResponse
    {
        public int RegistrosProcesados { get; set; }
        public int RegistrosErrores { get; set; }        
        public int RegistrosProcesadosDetalle { get; set; }
        public string registrosNit {get; set; }
        public string tregistrosGeneraError {get; set; }
        public string GeneraError { get; set; }
        public byte[] archivoTXTErrores { get; set; }
        public byte[] archivoTXTOk { get; set; }

    }
}
