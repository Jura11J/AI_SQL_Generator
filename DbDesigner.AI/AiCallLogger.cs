using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DbDesigner.Core.Schema;
using DbDesigner.Core.SchemaChanges;

namespace DbDesigner.AI;

public static class AiCallLogger
{
    private static readonly object Sync = new();

    public static Task LogAsync(AiCallLogEntry entry)
    {
        try
        {
            var logPath = GetLogPath();
            var directory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var lines = BuildLines(entry);
            lock (Sync)
            {
                File.AppendAllText(logPath, lines);
            }
        }
        catch
        {
            // Logging is best-effort; never throw.
        }

        return Task.CompletedTask;
    }

    private static string GetLogPath() =>
        Path.Combine(AppContext.BaseDirectory, "Logs", $"api_{DateTime.Now:yyyy-MM-dd}.log");

    private static string BuildLines(AiCallLogEntry entry)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]");
        sb.AppendLine($"Backend: {entry.Backend}");
        sb.AppendLine($"Schema: tables={entry.TableCount}, views={entry.ViewCount}");
        sb.AppendLine($"Specification: {entry.Specification}");

        if (!string.IsNullOrWhiteSpace(entry.RequestBody))
        {
            sb.AppendLine("Request JSON:");
            sb.AppendLine(entry.RequestBody);
        }

        if (!string.IsNullOrWhiteSpace(entry.ResponseBody))
        {
            sb.AppendLine("Response JSON:");
            sb.AppendLine(entry.ResponseBody);
        }

        if (entry.Changes?.Any() == true)
        {
            sb.AppendLine("Parsed Changes:");
            foreach (var change in entry.Changes)
            {
                sb.AppendLine($"- {change.GetType().Name}: {change.Description}");
            }
        }

        if (!string.IsNullOrWhiteSpace(entry.Error))
        {
            sb.AppendLine("Error:");
            sb.AppendLine(entry.Error);
        }

        sb.AppendLine(new string('-', 60));
        return sb.ToString();
    }
}

public class AiCallLogEntry
{
    public string Backend { get; set; } = string.Empty;
    public int TableCount { get; set; }
    public int ViewCount { get; set; }
    public string Specification { get; set; } = string.Empty;
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public string? Error { get; set; }
    public IEnumerable<SchemaChange>? Changes { get; set; }
}
