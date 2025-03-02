using IVEBA_API_Rest.Helpers;
using IVEBA_API_Rest.Models.IVEBA05;
using IVEBA_API_Rest.Services.IVEBA05;
using IVEBA_API_Rest.Utilidades;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace IVEBA_API_Rest.Services.IVE17DV
{
    public class IVEBA05HelperService : IIVEBA05HelperService
    {
        private readonly DbHelper _dbHelper;
        private readonly UtilidadesAPP utilidades;
        public IVEBA05HelperService(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
            utilidades = new UtilidadesAPP();
        }

        public async Task<DTO_IVEBA05Response> GenerarArchivoIVEBA05(int mes, int ano)
        {
            int cantidadRegistrosOK = 0, cantidadRegistrosError = 0;
            DTO_IVEBA05Response respuesta = new DTO_IVEBA05Response();

            try
            {
                // Eliminar registros temporales previos
                _dbHelper.ExecuteNonQuery("DELETE FROM IVE_BA_05_Temporal");

                // Consultar registros desde la base de datos
                List<DTO_IVEBA05Archivos> registros = ConsultarArchivosPorMesAno(mes, ano);

                if (registros.Count == 0)
                {
                    throw new Exception("No existe información para la fecha indicada.");
                }

                // Definir el nombre del archivo temporal
                string filePath = Path.Combine(Path.GetTempPath(), "archivoIVEBA05.txt");

                using (StreamWriter archivo = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    foreach (var registro in registros)
                    {
                        string stringGrabar = registro.StringValue;
                        // Se quitó la validación de si el cliente ya existia 
                        // Determinar tipo de cliente
                        switch (registro.Ordinal)
                        {
                            case 2:
                            case 3:
                                if (ProcesoFisicos(registro.Archivo, out string datosPersona))
                                {
                                    InsertarRegistroTemporal(registro.Archivo, datosPersona, registro);
                                }
                                else
                                {
                                    cantidadRegistrosError++;
                                    InsertarRegistroTemporal(registro.Archivo, "ERROR", null);
                                }
                                break;

                            case 4:
                                if (ProcesoJuridicos(registro.Archivo, out string datosEmpresa))
                                {
                                    InsertarRegistroTemporal(registro.Archivo, datosEmpresa, registro);
                                }
                                else
                                {
                                    cantidadRegistrosError++;
                                    InsertarRegistroTemporal(registro.Archivo, "ERROR", null);
                                }
                                break;

                            default:
                                throw new Exception($"Cliente {registro.Archivo} sin tipo definido.");
                        }

                        // Escribir en el archivo
                        archivo.WriteLine(stringGrabar);
                        cantidadRegistrosOK++;
                    }
                }

                // Leer el archivo en byte[]
                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);

                // Eliminar archivo temporal
                File.Delete(filePath);

                // Retornar respuesta
                respuesta.cantidadRegistrosOK = cantidadRegistrosOK;
                respuesta.cantidadRegistrosError = cantidadRegistrosError;
                respuesta.archivoTXT = fileBytes;
            }
            catch (Exception ex)
            {
                throw new Exception("Error en GenerarArchivoIVEBA05: " + ex.Message);
            }

            return respuesta;
        }

        public List<DTO_IVEBA05Archivos> ConsultarArchivosPorMesAno(int mes, int ano)
        {
            List<DTO_IVEBA05Archivos> listaDatos = new List<DTO_IVEBA05Archivos>();

            string query = @"SELECT * FROM IVE_BA_05_Archivos 
                             WHERE Mes = @Mes AND Ano = @Ano";

            SqlParameter[] parameters = {
                new SqlParameter("@Mes", mes),
                new SqlParameter("@Ano", ano)
            };

            try
            {
                DataTable dt = _dbHelper.ExecuteSelectCommand(query, parameters);

                foreach (DataRow row in dt.Rows)
                {
                    listaDatos.Add(new DTO_IVEBA05Archivos
                    {
                        Fecha = row["Fecha"].ToString(),
                        Archivo = row["Archivo"].ToString(),
                        Ordinal = Convert.ToInt32(row["Ordinal"]),
                        Mes = Convert.ToInt32(row["Mes"]),
                        Ano = Convert.ToInt32(row["Ano"]),
                        StringValue = row["String"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error en ConsultarArchivosPorMesAno: " + ex.Message);
            }

            return listaDatos;
        }

        private void InsertarRegistroTemporal(string cliente, string datos, DTO_IVEBA05Archivos registro)
        {
            string query = "INSERT INTO IVE_BA_05_TEMPORAL (Cliente, Datos, Dia, Mes, Ano) VALUES (@Cliente, @Datos, @Dia, @Mes, @Ano)";
            SqlParameter[] parameters = {
                new SqlParameter("@Cliente", cliente),
                new SqlParameter("@Datos", datos),
                new SqlParameter("@Dia", registro?.Mes ?? 99),
                new SqlParameter("@Mes", registro?.Mes ?? 99),
                new SqlParameter("@Ano", registro?.Ano ?? 9999)
            };

            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        private bool ProcesoFisicos(string cliente, out string datosPersona)
        {
            datosPersona = "";
            string query = $"SELECT * FROM dwcliente WHERE cod_cliente = {cliente}";
            DataTable dt = _dbHelper.ExecuteSelectCommand(query);

            if (dt.Rows.Count == 0)
                return false;

            DataRow row = dt.Rows[0];
            string tipoIdentificacion = row["TipoIdentificacion"].ToString();

            StringBuilder sb = new StringBuilder("I");

            switch (tipoIdentificacion)
            {
                case "1": // Cedula
                    string orden = row["Identificacion"].ToString().Substring(0, 3);
                    sb.Append("C").Append(FormateoString(orden, 3, " ")).Append(FormateoString(row["Identificacion"].ToString().Substring(4, 7), 15, " "));
                    break;

                case "2": // Partida
                    sb.Append("N   ").Append(FormateoString(row["Identificacion"].ToString(), 15, " "));
                    break;

                case "4":
                case "22": // Pasaporte
                    sb.Append("P   ").Append(FormateoString(row["Identificacion"].ToString(), 15, " "));
                    break;

                case "26": // DPI
                    sb.Append("D   ").Append(FormateoString(row["Identificacion"].ToString(), 15, " "));
                    break;

                default:
                    sb.Append("O   ").Append(FormateoString(row["Identificacion"].ToString(), 15, " "));
                    break;
            }

            sb.Append(FormateoString(row["Apellido1"].ToString(), 15, " "))
              .Append(FormateoString(row["Apellido2"].ToString(), 15, " "))
              .Append(FormateoString(row["ApellidoCasada"].ToString(), 15, " "))
              .Append(FormateoString(row["Nombre1"].ToString(), 15, " "))
              .Append(FormateoString(row["Nombre2"].ToString(), 15, " "))
              .Append(row["fnacimiento"].ToString())
              .Append("GT");

            datosPersona = sb.ToString();
            return true;
        }

        private bool ProcesoJuridicos(string cliente, out string datosEmpresa)
        {
            datosEmpresa = "";
            string query = $"SELECT * FROM DwCliente WHERE Cod_cliente = {cliente}";
            DataTable dt = _dbHelper.ExecuteSelectCommand(query);

            if (dt.Rows.Count == 0)
                return false;

            DataRow row = dt.Rows[0];
            string nitEmpresa = row["Nit"].ToString();

            if (string.IsNullOrEmpty(nitEmpresa))
            {
                nitEmpresa = row["TipoIdentificacion"].ToString() == "8" ? row["Identificacion"].ToString() : "SINNIT";
            }

            StringBuilder sb = new StringBuilder("J");
            sb.Append("N   ").Append(FormateoString(nitEmpresa, 15, " "))
              .Append(FormateoString(row["Nombrecliente"].ToString(), 75, " "))
              .Append(row["FNacimiento"].ToString())
              .Append("GT");

            datosEmpresa = sb.ToString();
            return true;
        }

        private string FormateoString(string input, int length, string pad, bool alignRight = false)
        {
            input = input.Trim();
            return alignRight ? input.PadRight(length, pad[0]) : input.PadLeft(length, pad[0]);
        }
    }
}
