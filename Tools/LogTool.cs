using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public static class LogTool
{
    [McpServerTool, Description("""
        Check Log Space: Execute DBCC SQLPERF(LOGSPACE) to report the current log file size and usage percentage for the specified database
        """)]
    public static string CheckLogSpace()
    {
        var command = new SqlCommand("DBCC SQLPERF(LOGSPACE)");

        return DbHelper.ExecuteDataTable(command);
    }

    [McpServerTool, Description("""
        Get File Details: Use EXEC sp_helpfile to retrieve details about the given database’s log file (e.g., logical name, physical path).
        """)]
    public static string GetFileDetails(string databaseName)
    {
        var command = new SqlCommand($"USE [{databaseName}]; EXEC sp_helpfile;");

        return DbHelper.ExecuteDataTable(command);
    }

    [McpServerTool, Description("""
        Shrink Log File: Execute DBCC SHRINKFILE to reduce the log file size to a target size in MB (e.g., 100 MB, as specified).
        Set database recovery model to SIMPLE before shrinking the log file.
        The Log File Name must be the logical name of the file, not the physical file name. The file name has extensions .ldf
        """)]
    public static string ShrinkLogFile(string databaseName, string databaseLogFileName, int sizeInMB)
    {
        var command = new SqlCommand($"USE [{databaseName}]; ALTER DATABASE [{databaseName}] SET RECOVERY SIMPLE; DBCC SHRINKFILE ({databaseLogFileName}, {sizeInMB});");

        return DbHelper.ExecuteDataTable(command);
    }
}
