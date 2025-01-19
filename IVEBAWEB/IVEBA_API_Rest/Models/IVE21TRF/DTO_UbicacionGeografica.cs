namespace IVEBA_API_Rest.Models.IVE21TRF
{
    public class DTO_UbicacionGeografica
    {
        public short UbicacionGeoId { get; set; }
        public string NombrePais { get; set; }
        public string NombreDepartamento { get; set; }
        public string NombreMunicipio { get; set; }
        public short PaisId { get; set; }
        public short DepartamentoId { get; set; }
        public int MunicipioId { get; set; }
        public string CodOrbeMuni { get; set; }
        public string CodOrbePais { get; set; }
        public string CodSibMuni { get; set; }
        public short MRPeso { get; set; }
        public string CodINE { get; set; }
    }
}
