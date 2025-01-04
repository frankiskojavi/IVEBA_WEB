namespace IVEBA_API_Rest.Models.IVE21TRF
{
    public class DTO_IVE21TRF
    {
        // Propiedades de IVE21Transferencia
        public DateTime? TRFFECHA { get; set; }
        public string TRFTIPO { get; set; }
        public string TRFTRAN { get; set; }
        public string TRFOCUN { get; set; }
        public string TRFOTPER { get; set; }
        public string TRFOTID { get; set; }
        public string TRFOORD { get; set; }
        public string TRFODOC { get; set; }
        public string TRFOMUN { get; set; }
        public string TRFOAPE1 { get; set; }
        public string TRFOAPE2 { get; set; }
        public string TRFOAPEC { get; set; }
        public string TRFONOM1 { get; set; }
        public string TRFONOM2 { get; set; }
        public string TRFOCTA { get; set; }
        public string TRFBCUN { get; set; }
        public string TRFBTPER { get; set; }
        public string TRFBTID { get; set; }
        public string TRFBORD { get; set; }
        public string TRFBDOC { get; set; }
        public string TRFBMUN { get; set; }
        public string TRFBAPE1 { get; set; }
        public string TRFBAPE2 { get; set; }
        public string TRFBAPEC { get; set; }
        public string TRFBNOM1 { get; set; }
        public string TRFBNOM2 { get; set; }
        public string TRFBCTA { get; set; }
        public int? TRFBBCO { get; set; }
        public string TRFNUM { get; set; }
        public string TRFPAIS { get; set; }
        public string TRFODEPT { get; set; }
        public string TRFDDEPT { get; set; }
        public string TRFBRN { get; set; }
        public decimal? TRFMNT { get; set; }
        public string TRFCCY { get; set; }
        public decimal? TRFMNTD { get; set; }
        public string Ive21 { get; set; }
        public long ID { get; set; }

        // Propiedades de DWAGENCIA
        public short? AgenciaId { get; set; }
        public string NombreAgencia { get; set; }
        public string DireccionAgencia { get; set; }
        public short? DepartamentoAgencia { get; set; }
        public int? MunicipioAgencia { get; set; }
        public string HorarioAgencia { get; set; }
        public string AutoBanco { get; set; }
        public string ATM { get; set; }
        public string TipoAgencia { get; set; }
        public byte? RegionId { get; set; }
        public byte? Banca { get; set; }
        public DateTime? Fapertura { get; set; }
        public string Telefono { get; set; }
        public string Fax { get; set; }
        public string CodigoPostal { get; set; }
        public short? CodigoInterno { get; set; }
        public short? Traslado { get; set; }
        public short? UbicacionGeoId { get; set; }

        // Propiedades de dwcuenta_iban
        public int? IbanIdOrigen { get; set; }
        public long? CuentaOrigen { get; set; }
        public string IbanOrigen { get; set; }
        public string IbanFormatoOrigen { get; set; }

        // Propiedades de dwcuenta_ibanben
        public int? IbanIdBeneficiario { get; set; }
        public long? CuentaBeneficiario { get; set; }
        public string IbanBeneficiario { get; set; }
        public string IbanFormatoBeneficiario { get; set; }
    }
}
