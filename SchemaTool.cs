using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

[McpServerToolType]
public static class SchemaTool
{
    [McpServerTool, Description("""
        Get data from a given query. Can filter by column values or sort by specific columns. The query must be a valid SQL SELECT statement.
        The object in query must exist in the database. The query parameters must be valid SQL parameters.
        """)]
    public static string ExecuteGetQueryData(string query, params SqlParameter[] queryParams)
    {
        var command = new SqlCommand(query);
        if (queryParams != null && queryParams.Length > 0)
        {
            foreach (var param in queryParams)
            {
                command.Parameters.Add(param);
            }
        }

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

    [McpServerTool, Description("""
        Gets column lists of a given table or all table in a database. Can filter columns by data type, precision, and scale.
        """)]
    public static string GetTableColumns(string? tableName = null,
        string? schemaName = null,
        string? dataType = null,
        int? precision = null,
        int? scale = null)
    {
        if (!string.IsNullOrEmpty(tableName) && tableName.Contains('.'))
        {
            // Split on the last '.' to support schema-qualified table names
            int lastDot = tableName.LastIndexOf('.');
            schemaName = tableName[..lastDot];
            tableName = tableName[(lastDot + 1)..];
        }

        var _tableQuery = new StringBuilder("""
                SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, NUMERIC_PRECISION, NUMERIC_SCALE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE 1 = 1 {WHERE_CONDITION}
                ORDER BY TABLE_NAME, COLUMN_NAME
                """);
        var command = new SqlCommand();
        var whereCondition = new StringBuilder();
        if (!string.IsNullOrEmpty(tableName))
        {
            whereCondition.Append("AND TABLE_NAME = @tableName ");
            command.Parameters.AddWithValue("@tableName", tableName);
        }

        if (!string.IsNullOrEmpty(schemaName))
        {
            whereCondition.Append("AND TABLE_SCHEMA = @schemaName ");
            command.Parameters.AddWithValue("@schemaName", schemaName);
        }

        if (!string.IsNullOrEmpty(dataType))
        {
            whereCondition.Append("AND DATA_TYPE = @dataType ");
            command.Parameters.AddWithValue("@dataType", dataType);
        }

        if (precision.HasValue)
        {
            whereCondition.Append("AND NUMERIC_PRECISION = @precision ");
            command.Parameters.AddWithValue("@precision", precision.Value);
        }

        if (scale.HasValue)
        {
            whereCondition.Append("AND NUMERIC_SCALE = @scale ");
            command.Parameters.AddWithValue("@scale", scale.Value);
        }

        _tableQuery = _tableQuery.Replace("{WHERE_CONDITION}", whereCondition.ToString());

        command.CommandText = _tableQuery.ToString();

        return Execute(command, (reader) =>
        {
            var results = new StringBuilder();

            // Markdown table header
            results.AppendLine("| Table Name | Column Name | Data Type | Precision | Scale |");
            results.AppendLine("|------------|-------------|-----------|-----------|-------|");

            // Process each row
            bool hasRows = false;
            while (reader.Read())
            {
                hasRows = true;
                // Escape pipe characters and ensure safe Markdown formatting
                string table = reader["TABLE_NAME"]?.ToString().Replace("|", "\\|") ?? "";
                string column = reader["COLUMN_NAME"]?.ToString().Replace("|", "\\|") ?? "";
                string dataTypeValue = reader["DATA_TYPE"]?.ToString().Replace("|", "\\|") ?? "";
                string precision = reader["NUMERIC_PRECISION"]?.ToString() ?? "";
                string scale = reader["NUMERIC_SCALE"]?.ToString() ?? "";

                results.AppendLine($"| {table} | {column} | {dataTypeValue} | {precision} | {scale} |");
            }

            return hasRows ? results.ToString() : "No matching columns found.";
        });
    }

    [McpServerTool, Description("Retrieves a list of all table names in the database.")]
    public static string GetTableList()
    {
        var _tableListQuery = """
            SELECT TABLE_SCHEMA, TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_NAME
            """;

        var cmd = new SqlCommand(_tableListQuery);
        return Execute(cmd, (reader) =>
        {
            var results = new StringBuilder();

            // Markdown table header
            results.AppendLine("| Schema | Table Name |");
            results.AppendLine("|--------|------------|");

            // Process each row
            bool hasRows = false;
            while (reader.Read())
            {
                hasRows = true;
                string schema = reader["TABLE_SCHEMA"]?.ToString().Replace("|", "\\|") ?? "";
                string table = reader["TABLE_NAME"]?.ToString().Replace("|", "\\|") ?? "";
                results.AppendLine($"| {schema} | {table} |");
            }

            return hasRows ? results.ToString() : "No tables found in the database.";
        });
    }

    private static string Execute(SqlCommand command, Func<SqlDataReader, string> readerFunc)
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
}

