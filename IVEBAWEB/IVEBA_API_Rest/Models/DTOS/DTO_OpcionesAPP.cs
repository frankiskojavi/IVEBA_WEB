using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IVEBA_API_Rest.Models.DTOS
{
    public class DTO_OpcionesAPP
    {
        public int codigoMenu { get; set; }
        public string? menuTitulo { get; set; }
        public string? menuDescripcion { get; set; }
        public string? menuBoton { get; set; }
        public string? menuImagen { get; set; }
        public string? menuPagina { get; set; }

    }
}