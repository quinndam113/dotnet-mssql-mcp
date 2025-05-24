using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public static class TimeTool
{
    [McpServerTool, Description("""
        Get current date and time. The result is in UTC format.
        """)]
    public static string GetDataTimeUtc()
    {
        return DateTime.Now.ToFileTimeUtc().ToString();
    }

    [McpServerTool, Description("""
        Get current date and time at local. format in current time zone
        """)]
    public static string GetDataTimeLocal()
    {
        return DateTime.Now.ToString();
    }

    [McpServerTool, Description("""
        Get current Time Zone
        """)]
    public static string GetTimeZone()
    {
        TimeZoneInfo localZone = TimeZoneInfo.Local;
        return ($"Local Time Zone ID: {localZone.Id} - Display Name: {localZone.DisplayName}");
    }
}
