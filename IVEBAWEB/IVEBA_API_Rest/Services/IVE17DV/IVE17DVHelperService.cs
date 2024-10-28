using IVEBA_API_Rest.Helpers;
using IVEBA_API_Rest.Models.IVE14EF;
using IVEBA_API_Rest.Models.IVE17DV;
using IVEBA_API_Rest.Utilidades;
using System.Data;
using System.Data.SqlClient;

namespace IVEBA_API_Rest.Services.IVE17DV
{
    public class IVE17DVHelperService : IIVE17DVHelperService
    {
        private readonly DbHelper _dbHelper;
        private readonly UtilidadesAPP utilidades;
        public IVE17DVHelperService(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
            utilidades = new UtilidadesAPP();
        }

        public async Task<DTO_IVE17DVResponse> GeneracionArchivoIVE17DV(int fechaInicial, int fechaFinal, bool archivoDefinitivo)
        {
            int cantidadRegistrosOK = 0;
            DTO_IVE17DVResponse respuesta = new DTO_IVE17DVResponse();            
            string caracter = "<>";

            try
            {
                // Verificar si se debe generar el archivo definitivo
                if (archivoDefinitivo)
                {
                    // Consulta SQL para obtener los registros                
                    List<DTO_IVE17DV> registros = ConsultarIVE17DVRangoFechas(fechaInicial, fechaFinal);

                    // Definir el nombre del archivo temporal
                    string filePath = Path.Combine(Path.GetTempPath(), "archivoGenerado.txt");

                    if (registros.Count > 0)
                    {
                        using (StreamWriter archivo = new StreamWriter(filePath))
                        {
                            foreach (var registro in registros)
                            {
                                // Construir cada línea de registro
                                string stringGrabar = $"{registro.Fecha}{caracter}" +
                                                      $"{registro.TipoTransaccion}{caracter}" +
                                                      $"{registro.TipoPersona}{caracter}" +
                                                      $"{registro.TipoIdentificacion}{caracter}";

                                // Eliminar ceros de NoOrden si corresponde
                                string noOrden = registro.NoOrden;
                                if (!string.IsNullOrEmpty(noOrden) && noOrden != "J10" && noOrden != "S20")
                                {
                                    noOrden = noOrden.Replace("0", "");
                                }

                                stringGrabar += $"{noOrden}{caracter}" +
                                                $"{registro.NoIdentificacion}{caracter}" +
                                                $"{registro.MuniEmiCedula}{caracter}" +
                                                $"{registro.Apellido1}{caracter}" +
                                                $"{registro.Apellido2}{caracter}" +
                                                $"{registro.ApellidoCasada}{caracter}" +
                                                $"{registro.Nombre1}{caracter}" +
                                                $"{registro.Nombre2}{caracter}" +
                                                $"{registro.NombreEmpresa}{caracter}" +
                                                $"{registro.FNacimiento}{caracter}" +
                                                $"{registro.PaisPersona}{caracter}" +
                                                $"{registro.ActividadEco}{caracter}" +
                                                $"{registro.Detalle}{caracter}" +
                                                $"{registro.Zona}{caracter}" +
                                                $"{registro.Depto}{caracter}" +
                                                $"{registro.Municipio}{caracter}" +
                                                $"{registro.OrigenFondos}{caracter}" +
                                                $"{registro.TipoMoneda}{caracter}" +
                                                $"{registro.MontoOriginal}{caracter}" +
                                                $"{registro.MontoD}{caracter}" +
                                                $"{registro.Agencia}{caracter}";

                                // Quitar tildes y reemplazar el separador
                                stringGrabar = utilidades.QuitoTildes(stringGrabar).Replace(caracter, "&&");

                                // Escribir la línea en el archivo
                                archivo.WriteLine(stringGrabar);
                                cantidadRegistrosOK++;
                            }
                        }

                        // Leer y retornar el archivo como arreglo de bytes
                        byte[] fileBytes = File.ReadAllBytes(filePath);

                        // Eliminar el archivo temporal
                        File.Delete(filePath);

                        respuesta.cantidadRegistrosOK = cantidadRegistrosOK;
                        respuesta.cantidadRegistrosError = 0;
                        respuesta.archivoTXT = fileBytes;
                    }
                }
            }catch(Exception ex)
            {
                throw new Exception("Error en GeneracionArchivoIVE17DV : " + ex.Message);
            }
            return respuesta;

        }
        public List<DTO_IVE17DV> ConsultarIVE17DVRangoFechas(int fechaInicial, int fechaFinal)
        {
            List<DTO_IVE17DV> listaDatos = new List<DTO_IVE17DV>();
            string query = "SELECT * FROM IVE17 WHERE Fecha between @FechaInicial and @FechaFinal";
            SqlParameter[] parameters = {
                new SqlParameter("@FechaInicial", fechaInicial),
                new SqlParameter("@FechaFinal", fechaFinal)
            };

            try
            {
                DataTable dt = _dbHelper.ExecuteSelectCommand(query, parameters);

                foreach (DataRow row in dt.Rows)
                {
                    listaDatos.Add(new DTO_IVE17DV
                    {
                        Fecha = row["Fecha"].ToString(),
                        TipoTransaccion = row["TipoTransaccion"].ToString(),
                        TipoPersona = row["TipoPersona"].ToString(),
                        TipoIdentificacion = row["TipoIdentificacion"].ToString(),
                        NoOrden = row["NoOrden"].ToString(),
                        NoIdentificacion = row["NoIdentificacion"].ToString(),
                        MuniEmiCedula = row["MuniEmiCedula"].ToString(),
                        Apellido1 = row["Apellido1"].ToString(),
                        Apellido2 = row["Apellido2"].ToString(),
                        ApellidoCasada = row["ApellidoCasada"].ToString(),
                        Nombre1 = row["Nombre1"].ToString(),
                        Nombre2 = row["Nombre2"].ToString(),
                        NombreEmpresa = row["NombreEmpresa"].ToString(),
                        FNacimiento = row["FNacimiento"].ToString(),
                        PaisPersona = row["PaisPersona"].ToString(),
                        ActividadEco = row["ActividadEco"].ToString(),
                        Detalle = row["Detalle"].ToString(),
                        Zona = row["Zona"].ToString(),
                        Depto = row["Depto"].ToString(),
                        Municipio = row["Municipio"].ToString(),
                        OrigenFondos = row["OrigenFondos"].ToString(),
                        TipoMoneda = row["TipoMoneda"].ToString(),
                        MontoOriginal = row["MontoOriginal"].ToString(),
                        MontoD = row["MontoD"].ToString(),
                        Agencia = row["Agencia"].ToString(),
                        Usuario = row["Usuario"].ToString(),
                        Asiento = row["Asiento"].ToString(),
                        Documento = row["Documento"].ToString(),
                        CodCliente = long.Parse(row["COD_CLIENTE"].ToString()),
                        NombreCliente = row["NOMBRECLIENTE"].ToString(),
                        DescripcionTrn = row["DESCRIPCIONTRN"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error en ConsultarIVE17DVRangoFechas : " + ex.Message);
            }
            

            return listaDatos;
        }        
    }
}
