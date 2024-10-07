using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IVEBA_API_Rest.Helpers;
using IVEBA_API_Rest.Models.DTOS;

namespace IVEBA_API_Rest.Services.SeguridadAPP
{
    public class SeguridadaAPPService : iSeguridadaAPPService
    {
        private readonly DbHelper _dbHelper;
        private readonly IConfiguration _configuration;

        public SeguridadaAPPService(DbHelper dbHelper, IConfiguration configuration)
        {
            _dbHelper = dbHelper;
            _configuration = configuration;
        }

        public async Task<List<DTO_OpcionesAPP>> ConsultarOpcionesMenuWebApp()
        {
            List<DTO_OpcionesAPP> listaOpcionesUsuario = new List<DTO_OpcionesAPP>();

            // Leer la sección "INFORMACION_MENU" desde el appsettings.json
            var menuSection = _configuration.GetSection("OpcionesMenu");
            if (menuSection.Exists())
            {
                try
                {
                    // Deserializar la sección "INFORMACION_MENU" a una lista de DTO_OpcionesAPP
                    listaOpcionesUsuario = menuSection.Get<List<DTO_OpcionesAPP>>();
                }
                catch (Exception ex)
                {                    
                    Console.WriteLine("Error al deserializar el JSON con las opciones del Menu : " + ex.Message);
                }
            }

            return listaOpcionesUsuario;
        }
    }
}