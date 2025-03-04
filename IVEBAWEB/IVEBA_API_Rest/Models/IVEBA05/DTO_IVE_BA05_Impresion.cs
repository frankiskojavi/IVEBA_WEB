namespace IVEBA_API_Rest.Models.IVEBA05
{
    public class DTO_IVE_BA05_Impresion
    {
        public short Sucursal { get; set; }
        public long Numero { get; set; }
        public DateTime Fecha { get; set; }
        public long Asiento { get; set; }
        public double Cliente { get; set; }
        public string Estado { get; set; } = string.Empty;
        public short Moneda { get; set; }
        public double MontoQ { get; set; }
        public double MontoD { get; set; }
        public short Transaccion { get; set; }
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public string Informacion { get; set; } = string.Empty;
        public short Opcion { get; set; }
        public string Datos { get; set; } = string.Empty;
        public string Iban { get; set; } = string.Empty;
    }

}
