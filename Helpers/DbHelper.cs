using Microsoft.Data.SqlClient;

internal static class DbHelper
{
    internal static string Execute(SqlCommand command, Func<SqlDataReader, string> readerFunc)
    {
        try
        {
            string connectionString = Environment.GetEnvironmentVariable("ConnectionString");
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            command.Connection = connection;
            using var reader = command.ExecuteReader();
            return readerFunc(reader);
        }
        catch (SqlException ex)
        {
            return $"Error executing query: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Unexpected error: {ex.Message}";
        }
    }

    internal static string ExecuteInsertUpdate(SqlCommand command)
    {
        try
        {
            string connectionString = Environment.GetEnvironmentVariable("ConnectionString");
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            command.Connection = connection;
            var rowEffects = command.ExecuteNonQuery();
            return $"{rowEffects} record effective";
        }
        catch (SqlException ex)
        {
            return $"Error executing query: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Unexpected error: {ex.Message}";
        }
    }
}
