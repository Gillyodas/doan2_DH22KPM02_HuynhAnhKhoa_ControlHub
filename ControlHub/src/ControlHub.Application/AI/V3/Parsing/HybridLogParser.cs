using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ControlHub.Application.Common.Interfaces.AI;
using ControlHub.Application.Common.Interfaces.AI.V3.Parsing;
using ControlHub.Application.Common.Logging;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AI.V3.Parsing
{
    public class HybridLogParser : IHybridLogParser
    {
        private readonly ILogParserService _drainParser;
        private readonly ISemanticLogClassifier _semanticClassifier;
        private readonly ILogger<HybridLogParser> _logger;

        public HybridLogParser(
            ILogParserService drainParser,
            ISemanticLogClassifier semanticClassifier,
            ILogger<HybridLogParser> logger)
        {
            _drainParser = drainParser;
            _semanticClassifier = semanticClassifier;
            _logger = logger;
        }

        public async Task<HybridParseResult> ParseLogsAsync(
            List<LogEntry> logs, 
            HybridParsingOptions? options = null, 
            CancellationToken ct = default)
        {
            options ??= new HybridParsingOptions();
            var sw = Stopwatch.StartNew();
            
            var templates = new List<LogTemplate>();
            var templateToLogs = new Dictionary<string, List<LogEntry>>();
            int drainCount = 0;
            int semanticCount = 0;
            int failedCount = 0;
            float totalConfidence = 0f;

            // Step 1: Drain3 Parsing for the whole batch
            var drainResult = await _drainParser.ParseLogsAsync(logs);
            
            foreach (var drainTemplate in drainResult.Templates)
            {
                var templateLogs = drainResult.TemplateToLogs[drainTemplate.TemplateId];
                var confidence = CalculateDrainConfidence(drainTemplate);

                if (confidence >= options.ConfidenceThreshold || !options.EnableSemantic || semanticCount >= options.MaxSemanticLogs)
                {
                    // Keep Drain3 result
                    templates.Add(drainTemplate);
                    templateToLogs[drainTemplate.TemplateId] = templateLogs;
                    drainCount += templateLogs.Count;
                    totalConfidence += confidence * templateLogs.Count;
                }
                else
                {
                    // Fallback to Semantic for each log in this "low confidence" cluster
                    foreach (var log in templateLogs)
                    {
                        var semanticResult = await _semanticClassifier.ClassifyAsync(log.Message, ct);
                        
                        // Create a specific template for this semantic category if it doesn't exist
                        var semanticTemplateId = $"semantic_{semanticResult.Category}";
                        var existingTemplate = templates.FirstOrDefault(t => t.TemplateId == semanticTemplateId);
                        
                        if (existingTemplate == null)
                        {
                            existingTemplate = new LogTemplate(
                                semanticTemplateId,
                                $"[Semantic: {semanticResult.Category}] <*>",
                                0,
                                log.Timestamp,
                                log.Timestamp,
                                MapCategoryToSeverity(semanticResult.Category)
                            );
                            templates.Add(existingTemplate);
                            templateToLogs[semanticTemplateId] = new List<LogEntry>();
                        }

                        // C# record immutability - use 'with' to "update" (wait, it's a list, we handle count later or replace)
                        // Actually, we'll just update the dictionary and rebuild list totals at the end
                        templateToLogs[semanticTemplateId].Add(log);
                        semanticCount++;
                        totalConfidence += semanticResult.Confidence;
                    }
                }
            }

            sw.Stop();

            var metadata = new ParsingMetadata(
                Drain3Count: drainCount,
                SemanticCount: semanticCount,
                FailedCount: failedCount,
                AverageConfidence: (drainCount + semanticCount) > 0 ? totalConfidence / (drainCount + semanticCount) : 0f,
                ProcessingTimeMs: sw.ElapsedMilliseconds
            );

            return new HybridParseResult(templates, templateToLogs, metadata);
        }

        public async Task<ParsedLog> ParseSingleAsync(string logLine, CancellationToken ct = default)
        {
            var logEntry = new LogEntry { MessageTemplate = logLine, Timestamp = DateTime.UtcNow };
            var drainResult = await _drainParser.ParseLogsAsync(new List<LogEntry> { logEntry });
            
            var template = drainResult.Templates.FirstOrDefault();
            var confidence = template != null ? CalculateDrainConfidence(template) : 0f;

            if (confidence >= 0.7f)
            {
                return new ParsedLog(
                    logLine,
                    template?.Pattern ?? logLine,
                    new LogClassification("System", "General", confidence, new Dictionary<string, string>()),
                    ParsingMethod.Drain3,
                    confidence
                );
            }

            var semanticResult = await _semanticClassifier.ClassifyAsync(logLine, ct);
            return new ParsedLog(
                logLine,
                template?.Pattern ?? logLine,
                semanticResult,
                ParsingMethod.Semantic,
                semanticResult.Confidence
            );
        }

        private float CalculateDrainConfidence(LogTemplate template)
        {
            var wildcardCount = template.Pattern.Split("<*>").Length - 1;
            if (wildcardCount == 0) return 0.95f;
            if (wildcardCount == 1) return 0.85f;
            if (wildcardCount == 2) return 0.70f;
            return 0.50f;
        }

        private string MapCategoryToSeverity(string category)
        {
            return category.ToLower() switch
            {
                "authentication" => "Warning",
                "authorization" => "Warning",
                "database" => "Error",
                "network" => "Error",
                _ => "Information"
            };
        }
    }
}
