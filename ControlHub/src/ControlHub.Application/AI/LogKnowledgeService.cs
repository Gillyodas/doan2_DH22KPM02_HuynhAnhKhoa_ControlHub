using System.Reflection;
using System.Text;
using ControlHub.Application.Common.Interfaces.AI;
using ControlHub.Application.Common.Logging;
using ControlHub.SharedKernel.Common.Logs; // Để truy cập LogCode

namespace ControlHub.Application.AI
{
    public class LogKnowledgeService
    {
        private readonly IVectorDatabase _vectorDb;
        private readonly IEmbeddingService _embeddingService;
        private readonly IAIAnalysisService _aiService;
        private const string CollectionName = "LogDefinitions";

        public LogKnowledgeService(
            IVectorDatabase vectorDb, 
            IEmbeddingService embeddingService, 
            IAIAnalysisService aiService)
        {
            _vectorDb = vectorDb;
            _embeddingService = embeddingService;
            _aiService = aiService;
        }

        // 1. INGESTION: Học các LogCode từ code
        public async Task IngestLogDefinitionsAsync()
        {
            // Scan toàn bộ assembly chứa CommonLogs để tìm các class XXXLogs
            var assembly = typeof(CommonLogs).Assembly;
            var logClasses = assembly.GetTypes()
                .Where(t => t.Name.EndsWith("Logs") && t.IsClass && t.IsSealed && t.IsAbstract == false); 
                // Lưu ý: C# static class là abstract sealed. Nhưng LogCode files của ta là public static class? Yes.
                // Điều kiện: tìm các class có tên *Logs.

            foreach (var type in logClasses)
            {
                // Lấy tất cả field static trả về LogCode
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => f.FieldType == typeof(LogCode));

                foreach (var field in fields)
                {
                    var logCode = (LogCode?)field.GetValue(null);
                    if (logCode == null) continue;

                    // Text để tạo vector: Cần chứa cả Code lẫn Message để AI hiểu ngữ nghĩa
                    var textToEmbed = $"Code: {logCode.Code}. Meaning: {logCode.Message}";
                    
                    // Tạo Embedding
                    var vector = await _embeddingService.GenerateEmbeddingAsync(textToEmbed);
                    
                    if (vector.Length == 0) continue; // Skip nếu lỗi

                    // Lưu vào Qdrant
                    var payload = new Dictionary<string, object>
                    {
                        { "Code", logCode.Code },
                        { "Message", logCode.Message },
                        { "SourceClass", type.Name }
                    };

                    // ID là Code string (unique)
                    await _vectorDb.UpsertAsync(CollectionName, logCode.Code, vector, payload);
                }
            }
        }

        // 2. RAG ANALYSIS: Phân tích log dựa trên kiến thức
        public async Task<string> AnalyzeSessionAsync(List<LogEntry> logs, string lang = "en")
        {
            if (!logs.Any()) return "No logs to analyze.";

            // Bước 2.1: Tìm ngữ cảnh (Context)
            // Lấy các LogCode unique xuất hiện trong session này
            var uniqueCodes = logs
                .Where(l => l.LogCode != null && !string.IsNullOrEmpty(l.LogCode.Code))
                .Select(l => l.LogCode!.Code)
                .Distinct()
                .ToList();

            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("Knowledge Context:");

            // Với mỗi LogCode xuất hiện, ta tìm trong Vector DB xem có thông tin gì thêm không
            // (Thực ra nếu ta đã có Message trong Log rồi thì cũng đã đủ. 
            // Nhưng RAG mạnh ở chỗ nếu ta lưu thêm "Cách Fix" trong Vector DB thì sẽ retrieve được).
            // Ở đây demo: Ta search Vector DB bằng chính Log Message để xem có "LogCode tương tự" nào khác không (ít tác dụng nếu data ít).
            // Tốt hơn: Search Vector Database để tìm "Tài liệu xử lý lỗi" (Documentation).
            
            // DEMO SIMPLE RAG:
            // Lấy 1 log lỗi đầu tiên và tìm kiếm trong DB xem có LogCode nào tương tự (để AI hiểu nhóm lỗi)
            var errorLog = logs.FirstOrDefault(l => l.Level == "Error" || l.Level == "Warning");
            if (errorLog != null)
            {
                var searchVector = await _embeddingService.GenerateEmbeddingAsync(errorLog.Message);
                if (searchVector.Length > 0)
                {
                    var relatedDocs = await _vectorDb.SearchAsync(CollectionName, searchVector, limit: 2);
                    foreach (var doc in relatedDocs)
                    {
                        if (doc.Payload.ContainsKey("Message"))
                        {
                            contextBuilder.AppendLine($"- Related Concept: {doc.Payload["Code"]} ({doc.Payload["Message"]})");
                        }
                    }
                }
            }

            // Bước 2.2: Optimize & Build Prompt
            var prompt = new StringBuilder();
            
            // Map Language Code to Full Name
            string languageName = lang.ToLower() switch
            {
                "vi" => "Vietnamese",
                "vn" => "Vietnamese", // Handle common mistake
                "ja" => "Japanese",
                "ko" => "Korean",
                "zh" => "Chinese",
                "fr" => "French",
                "es" => "Spanish",
                "de" => "German",
                _ => "English"
            };

            prompt.AppendLine("You are an expert system troubleshooter assistant.");
            prompt.AppendLine($"IMPORTANT: You MUST respond in {languageName}.");
            prompt.AppendLine("IMPORTANT: The Context Log Definitions may contain templates with placeholders (e.g. {Code}, {UserId}). You MUST replace them with the actual values found in the Log Sequence.");
            prompt.AppendLine("If a log entry does not have a matching definition, analyze it based on the message content.");
            prompt.AppendLine("\nContext Logs:");

            // OPTIMIZATION: Filter & Truncate
            // 1. Prioritize Errors & Warnings
            var criticalLogs = logs.Where(l => l.Level == "Error" || l.Level == "Warning").ToList();
            
            // 2. Get recent Context (Information) - Max 20 last entries
            var infoLogs = logs.Where(l => l.Level != "Error" && l.Level != "Warning")
                               .TakeLast(20)
                               .ToList();

            // 3. Combine & Sort by Timestamp
            var logsToAnalyze = criticalLogs.Concat(infoLogs)
                                            .OrderBy(l => l.Timestamp)
                                            .TakeLast(50) // Absolute Hard Cap
                                            .ToList();

            foreach (var log in logsToAnalyze)
            {
                // Truncate message to avoid massive stack traces (Max 500 chars)
                var cleanMessage = log.Message?.Length > 500 
                    ? log.Message.Substring(0, 500) + "...[TRUNCATED]" 
                    : log.Message;

                // FIX: Don't print "NoCode" if LogCode is missing. 
                var codeDisplay = log.LogCode?.Code != null ? $" {log.LogCode.Code}:" : "";

                prompt.AppendLine($"[{log.Timestamp:HH:mm:ss}] [{log.Level}]{codeDisplay} {cleanMessage}");
            }

            // Bước 2.3: Gọi AI
            return await _aiService.AnalyzeLogsAsync(prompt.ToString());
        }

        // 3. CHAT WITH LOGS: Hỏi đáp với log
        public async Task<string> ChatWithLogsAsync(string userQuestion, List<LogEntry> logs, string lang = "en")
        {
            var prompt = new StringBuilder();

            // Map Language Code to Full Name
            string languageName = lang.ToLower() switch
            {
                "vi" => "Vietnamese",
                "vn" => "Vietnamese", 
                "ja" => "Japanese",
                "ko" => "Korean",
                "zh" => "Chinese",
                "fr" => "French",
                "es" => "Spanish",
                "de" => "German",
                _ => "English"
            };

            prompt.AppendLine("You are an expert system troubleshooter assistant.");
            prompt.AppendLine($"IMPORTANT: You MUST respond in {languageName}.");
            prompt.AppendLine("\nContext Logs:");

            // Filter & Truncate for Chat Context
            // Prioritize recent logs (last 50) regarding constraints
            var logsToAnalyze = logs.OrderBy(l => l.Timestamp)
                                    .TakeLast(50) 
                                    .ToList();

            foreach (var log in logsToAnalyze)
            {
                 // Handling "NoCode" gracefully: If LogCode is null, don't show it.
                 var codeDisplay = log.LogCode?.Code != null ? $"[{log.LogCode.Code}] " : "";
                 var cleanMessage = log.Message?.Length > 300 
                    ? log.Message.Substring(0, 300) + "..." 
                    : log.Message;

                prompt.AppendLine($"[{log.Timestamp:HH:mm:ss}] [{log.Level}] {codeDisplay}{cleanMessage}");
            }

            prompt.AppendLine($"\nUser Question: {userQuestion}");
            prompt.AppendLine("Answer based on the logs provided above:");

            return await _aiService.AnalyzeLogsAsync(prompt.ToString());
        }
    }
}
