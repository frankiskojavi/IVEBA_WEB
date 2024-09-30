using System.Data;
using System.Data.SqlClient;

namespace IVEBA_API_Rest.Helpers
{
    public class DbHelper
    {
        private readonly string _connectionString;

        public DbHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("IVEBA");
        }

        // Método para ejecutar consultas SELECT
        public DataTable ExecuteSelectCommand(string query, SqlParameter[] parameters = null)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.CommandTimeout = 500000;
                if (parameters != null)
                    command.Parameters.AddRange(parameters);

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable table = new DataTable();
                adapter.Fill(table);
                return table;
            }
        }

        // Método para ejecutar comandos de INSERT, UPDATE, DELETE
        public int ExecuteNonQuery(string query, SqlParameter[] parameters = null)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                if (parameters != null)
                    command.Parameters.AddRange(parameters);

                connection.Open();
                return command.ExecuteNonQuery();
            }
        }
    }
}