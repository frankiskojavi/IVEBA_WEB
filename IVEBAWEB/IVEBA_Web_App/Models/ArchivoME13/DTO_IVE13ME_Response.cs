namespace IVEBA_Web_App.Models.ArchivoME13
{
    public class DTO_IVE13ME_Response
    {
        public string? LineaId { get; set; }
        public int? Fecha { get; set; }
        public string? Transaccion { get; set; }
        public string? Tipo_Moneda { get; set; }
        public decimal MontoMO { get; set; }
        public decimal MontoUSD { get; set; }
        public int? Cantidad_Trx { get; set; }
        public int? Agenciaid { get; set; }
        public int? ORDEN { get; set; }
    }
}
