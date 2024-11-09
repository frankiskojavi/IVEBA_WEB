namespace IVEBA_API_Rest.Models.IVECH
{
    public class DTO_CHCajaTemporalResponse
    {        
        public int registrosOKEncabezado { get; set; }        
        public int registrosErrorEncabezado { get; set; }
        public int registrosOKDetalle { get;set; }        
        public int registrosERRORDetalle { get; set; }
        public int cantidadNit { get; set; } 
        public byte[] archivoTXTErrores { get; set; }
        public byte[] archivoTXTOk { get; set; }

    }
}
