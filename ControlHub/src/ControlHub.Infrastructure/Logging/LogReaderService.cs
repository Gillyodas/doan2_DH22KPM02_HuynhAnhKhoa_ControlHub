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

            var matches = logs.Where(l =>
            (l.Properties.ContainsKey("CorrelationId") && l.Properties["CorrelationId"].ToString() == correlationId) ||
            (l.TraceId == correlationId) ||
            (l.RequestId == correlationId) ||
            (l.SerilogTraceId == correlationId)
            ).OrderBy(x => x.Timestamp).ToList();

            _logger.LogInformation("LogReader: Found {Count} matches for {CorrelationId}", matches.Count, correlationId);

            return matches;
        }

        public async Task<List<LogEntry>> GetRecentLogsAsync(int count = 100)
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
                                // Bỏ qua dòng lỗi
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