using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ControlHub.Application.Common.Logging;
using ControlHub.Application.Common.Logging.Interfaces;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Configuration;

namespace ControlHub.Infrastructure.Logging
{
    public class LogReaderService : ILogReaderService
    {
        private readonly string _logDirectory;
        private readonly ILogger<LogReaderService> _logger;

        public LogReaderService(ILogger<LogReaderService> logger, IConfiguration configuration)
        {
            var configuredPath = configuration["Logging:LogDirectory"];
            if (!string.IsNullOrEmpty(configuredPath))
            {
                _logDirectory = configuredPath;
            }
            else
            {
                // Updated to match Serilog's default working directory output
                _logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            }

            _logger = logger;
        }
        public async Task<List<LogEntry>> GetLogsByCorrelationIdAsync(string correlationId)
        {
            var logs = await ReadAllLogsAsync();
            
            _logger.LogInformation("LogReader: Read {Count} total logs from {Path}. Searching for {CorrelationId}", logs.Count, _logDirectory, correlationId);

            // Search in multiple fields since "CorrelationId" might not exist
            // ASP.NET Core uses RequestId (format: "0HNJ3P2CR9G6I:00000027")
            // User might provide:
            // - Full RequestId: "0HNJ3P2CR9G6I:00000027"
            // - Just connection ID: "0HNJ3P2CR9G6I"
            // - Just sequence number: "00000027"
            // - TraceId or SerilogTraceId
            var matches = logs.Where(l =>
                // Match RequestId (full, contains, startsWith, endsWith)
                (l.RequestId != null && (
                    l.RequestId == correlationId || 
                    l.RequestId.Contains(correlationId) ||
                    l.RequestId.StartsWith(correlationId) ||
                    l.RequestId.EndsWith(correlationId)
                )) ||
                // Match TraceId (exact or contains)
                (l.TraceId != null && (l.TraceId == correlationId || l.TraceId.Contains(correlationId))) ||
                // Match Serilog TraceId (@tr field)
                (l.SerilogTraceId != null && (l.SerilogTraceId == correlationId || l.SerilogTraceId.Contains(correlationId))) ||
                // Match in Properties if CorrelationId exists there
                (l.Properties.ContainsKey("CorrelationId") && l.Properties["CorrelationId"].ToString() == correlationId)
            ).OrderBy(x => x.Timestamp).ToList();

            _logger.LogInformation("LogReader: Found {Count} matches for {CorrelationId}", matches.Count, correlationId);

            return matches;
        }

        public async Task<List<LogEntry>> GetRecentLogsAsync(int count = 500)
        {
            var logs = await ReadAllLogsAsync();
            return logs.OrderByDescending(x => x.Timestamp).Take(count).ToList();
        }

        public async Task<List<LogEntry>> GetLogsByTimeRangeAsync(DateTime startTime, DateTime endTime)
        {
            var logs = await ReadAllLogsAsync();

            // Simple optimization: only process logs if needed
            return logs.Where(l => l.Timestamp >= startTime && l.Timestamp <= endTime)
                       .OrderBy(l => l.Timestamp)
                       .ToList();
        }

        private async Task<List<LogEntry>> ReadAllLogsAsync()
        {
            var result = new List<LogEntry>();

            if (!Directory.Exists(_logDirectory))
            {
                _logger.LogWarning("Log directory not found at {Path}", _logDirectory);
                return result;
            }

            var files = Directory.GetFiles(_logDirectory, "log-*.json");

            foreach(var file in files)
            {
                try
                {
                    using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                    {
                        string line;
                        while ((line = await streamReader.ReadLineAsync()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            try
                            {
                                var entry = JsonSerializer.Deserialize<LogEntry>(line);
                                if (entry != null) result.Add(entry);
                            }
                            catch
                            {
                                // B? qua dòng l?i
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Failed to read log file {File}", file);
                }
            }

            return result;
        }
    }
}
