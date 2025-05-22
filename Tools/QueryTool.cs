using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;


[McpServerToolType]
public static class QueryTool
{
    [McpServerTool, Description("""
        Get data from a given query. Can filter by column values or sort by specific columns. The query must be a valid SQL SELECT statement.
        The object in query must exist in the database.
        """)]
    public static string ExecuteGetQueryData(string query)
    {
        var command = new SqlCommand(query);

        return DbHelper.Execute(command, (reader) =>
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
        Execute insert, update or delete from a given query. The query must be a valid SQL SELECT statement.
        The object in query must exist in the database. The query parameters must be valid SQL parameters.
        """)]
    public static string ExecuteInsertUpdateData(string query)
    {
        var command = new SqlCommand(query);

        return DbHelper.ExecuteInsertUpdate(command);
    }
}
