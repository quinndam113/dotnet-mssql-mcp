using Microsoft.Data.SqlClient;
using System.Text;

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

    internal static string ExecuteDataTable(SqlCommand command)
    {
        try
        {
            return Execute(command, (reader) =>
            {
                var results = new StringBuilder();
                // Markdown table header
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    results.Append($"| {reader.GetName(i)} ");
                }
                results.AppendLine("|");
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    results.Append("|---");
                }
                results.AppendLine("|");
                // Process each row
                bool hasRows = false;
                while (reader.Read())
                {
                    hasRows = true;
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string value = reader[i]?.ToString().Replace("|", "\\|") ?? "";
                        results.Append($"| {value} ");
                    }
                    results.AppendLine("|");
                }
                return hasRows ? results.ToString() : "No data found.";
            });
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
