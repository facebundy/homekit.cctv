using Microsoft.Extensions.Logging;

namespace homekit.cctv.Models;

public class LogMessage
{
    public string Message { get; set; } = string.Empty;
    public LogLevel Level { get; set; }
    public string? Exception { get; set; }
    public DateTime Timestamp { get; set; }
}
