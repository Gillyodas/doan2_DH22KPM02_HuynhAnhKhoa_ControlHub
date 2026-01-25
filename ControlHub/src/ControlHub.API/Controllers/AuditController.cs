using ControlHub.Application.AI;
using ControlHub.Application.Common.Logging;
using ControlHub.Application.Common.Logging.Interfaces;
using ControlHub.Domain.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlHub.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly LogKnowledgeService _knowledgeService;
        private readonly ILogReaderService _logReader;

        public AuditController(LogKnowledgeService knowledgeService, ILogReaderService logReader)
        {
            _knowledgeService = knowledgeService;
            _logReader = logReader;
        }

        // Endpoint 1: Dạy cho AI biết về các Log Code hiện có (chạy 1 lần sau khi update code)
        [Authorize(Policy = Policies.CanManageSystemSettings)]
        [HttpPost("learn")]
        public async Task<IActionResult> LearnLogDefinitions()
        {
            await _knowledgeService.IngestLogDefinitionsAsync();
            return Ok("Ingestion started. Check Qdrant for vectors.");
        }

        // Endpoint 2: Phân tích Session
        [Authorize(Policy = Policies.CanViewSystemLogs)]
        [HttpGet("analyze/{correlationId}")]
        public async Task<IActionResult> Analyze(string correlationId, [FromQuery] string lang = "en")
        {
            // Auto-detect language from browser if not specified (or default "en")
            if (lang == "en" && Request.Headers.ContainsKey("Accept-Language"))
            {
                var header = Request.Headers["Accept-Language"].ToString();
                var firstLang = header.Split(',')[0].Split(';')[0]; // e.g., "vi-VN"
                lang = firstLang.Contains('-') ? firstLang.Split('-')[0] : firstLang; // "vi"
            }
            // 1. Lấy log gốc
            var logs = await _logReader.GetLogsByCorrelationIdAsync(correlationId);
            if (logs.Count == 0) return NotFound("Log session not found.");

            // 2. Chạy qua RAG Service
            var result = await _knowledgeService.AnalyzeSessionAsync(logs, lang);

            return Ok(new
            {
                CorrelationId = correlationId,
                Analysis = result,
                LogCount = logs.Count
            });
        }

        // Endpoint 3: Chat với Logs
        [Authorize(Policy = Policies.CanViewSystemLogs)]
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request, [FromQuery] string lang = "en")
        {
             // Auto-detect language
            if (lang == "en" && Request.Headers.ContainsKey("Accept-Language"))
            {
                var header = Request.Headers["Accept-Language"].ToString();
                var firstLang = header.Split(',')[0].Split(';')[0]; 
                lang = firstLang.Contains('-') ? firstLang.Split('-')[0] : firstLang;
            }

            // Default: Last 24 hours if not specified
            var endTime = request.EndTime ?? DateTime.UtcNow;
            var startTime = request.StartTime ?? endTime.AddHours(-24);

            // 1. Get Logs
            var logs = await _logReader.GetLogsByTimeRangeAsync(startTime, endTime);
            if (logs.Count == 0) return Ok(new { Answer = "No logs found in the specified time range." });

            // 2. Chat with AI
            var answer = await _knowledgeService.ChatWithLogsAsync(request.Question, logs, lang);

            return Ok(new
            {
                Question = request.Question,
                Answer = answer,
                LogCount = logs.Count
            });
        }
    }

    public class ChatRequest
    {
        public string Question { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
