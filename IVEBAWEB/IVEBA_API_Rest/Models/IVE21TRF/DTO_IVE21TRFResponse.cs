namespace IVEBA_API_Rest.Models.IVE17DV
{
    public class DTO_IVE21TRFResponse
    {
        public int registrosOKEncabezado { get; set; }
        public int registrosErrorEncabezado { get; set; }
        public int registrosOKDetalle { get; set; }
        public int registrosERRORDetalle { get; set; }
        public int cantidadNit { get; set; }
        public byte[]? archivoTXTErrores { get; set; }
        public byte[]? archivoTXTOk { get; set; }
    }
}
