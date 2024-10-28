namespace IVEBA_API_Rest.Models.IVE13ME
{
    public class DTO_IVE13MEResponse
    {        
        public int cantidadRegistrosOK { get; set;  } 
        public int cantidadRegistrosError { get; set;  } 
        public byte[] archivoTXT { get; set; }
    }
}
