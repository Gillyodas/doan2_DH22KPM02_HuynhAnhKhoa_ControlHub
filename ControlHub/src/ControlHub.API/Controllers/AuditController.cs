using ControlHub.Application.Common.Interfaces.AI;
using ControlHub.Application.Common.Interfaces.AI.V1;
using ControlHub.Application.Common.Interfaces.AI.V3;
using ControlHub.Application.Common.Interfaces.AI.V3.Observability;
using ControlHub.Application.Common.Logging.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlHub.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly ILogKnowledgeService _knowledgeService; // V1 service (interface)
        private readonly IAuditAgentService? _agentService;      // V2.5/V3 service (optional)
        private readonly IAuditAgentV3? _auditAgentV3;           // V3 full agentic (optional)
        private readonly IAgentObserver? _agentObserver;         // V3 tracing (optional)
        private readonly ILogReaderService _logReader;
        private readonly ILogParserService? _logParser;          // V2.5 parser
        private readonly IRunbookService? _runbookService;       // V2.5 runbooks
        private readonly IConfiguration _config;

        public AuditController(
            ILogKnowledgeService knowledgeService,
            ILogReaderService logReader,
            IServiceProvider sp, // Lazy resolve V2/V3 services
            IConfiguration config)
        {
            _knowledgeService = knowledgeService;
            _logReader = logReader;
            _config = config;

            // Resolve optional services if available
            _agentService = sp.GetService<IAuditAgentService>();
            _logParser = sp.GetService<ILogParserService>();
            _runbookService = sp.GetService<IRunbookService>();

            // V3 specific services
            _auditAgentV3 = sp.GetService<IAuditAgentV3>();
            _agentObserver = sp.GetService<IAgentObserver>();
        }

        // Endpoint 0: Check AI Version
        [AllowAnonymous] // Or minimal auth
        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            var version = _config["AuditAI:Version"] ?? "V1";
            var drain3 = _config.GetValue<bool>("AuditAI:Drain3Enabled");

            return Ok(new
            {
                Version = version,
                Features = new[]
                {
                    drain3 ? "Drain3 Parsing" : "Passive RAG",
                    _agentService != null ? "Agentic Workflow" : "Standard Workflow"
                }
            });
        }

        // Endpoint 1: Ingest Log Definitions (V1)
        [Authorize(Policy = "Permission:system.manage_settings")]
        [HttpPost("learn")]
        public async Task<IActionResult> LearnLogDefinitions()
        {
            await _knowledgeService.IngestLogDefinitionsAsync();
            return Ok("Log Definitions Ingestion started.");
        }

        // Endpoint 1.5: Ingest Runbooks (V2.5)
        [Authorize(Policy = "Permission:system.manage_settings")]
        [HttpPost("ingest-runbooks")]
        public async Task<IActionResult> IngestRunbooks([FromBody] List<RunbookEntry> runbooks)
        {
            if (_runbookService == null) return BadRequest("Runbook Service is not enabled (V2.5 required).");

            await _runbookService.IngestRunbooksAsync(runbooks);
            return Ok(new { Message = $"Ingested {runbooks.Count} runbooks." });
        }

        // Endpoint 2: Phân tích Session (Hybrid V1/2.5)
        [Authorize(Policy = "Permission:system.view_logs")]
        [HttpGet("analyze/{correlationId}")]
        public async Task<IActionResult> Analyze(string correlationId, [FromQuery] string lang = "en")
        {
            // Auto-detect language
            if (lang == "en" && Request.Headers.ContainsKey("Accept-Language"))
            {
                var header = Request.Headers["Accept-Language"].ToString();
                var firstLang = header.Split(',')[0].Split(';')[0];
                lang = firstLang.Contains('-') ? firstLang.Split('-')[0] : firstLang;
            }

            // Route to V2.5 Agent if available
            if (_agentService != null)
            {
                var auditResult = await _agentService.InvestigateSessionAsync(correlationId, lang);
                return Ok(new
                {
                    CorrelationId = correlationId,
                    Analysis = auditResult.Analysis,
                    ToolsUsed = auditResult.ToolsUsed,
                    Templates = auditResult.ProcessedTemplates, // Return templates for UI visualization
                    Version = "V2.5"
                });
            }

            // Fallback to V1
            var logs = await _logReader.GetLogsByCorrelationIdAsync(correlationId);
            if (logs.Count == 0) return NotFound("Log session not found.");

            var result = await _knowledgeService.AnalyzeSessionAsync(logs, lang);

            return Ok(new
            {
                CorrelationId = correlationId,
                Analysis = result,
                LogCount = logs.Count,
                Version = "V1"
            });
        }

        // Endpoint 3: Chat với Logs (Hybrid V1/2.5)
        [Authorize(Policy = "Permission:system.view_logs")]
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] Application.Common.Interfaces.AI.ChatRequest request, [FromQuery] string lang = "en")
        {
            // ─────────────────────────────────────────────────────────
            // Auto-detect language from header
            // ─────────────────────────────────────────────────────────
            if (lang == "en" && Request.Headers.ContainsKey("Accept-Language"))
            {
                var header = Request.Headers["Accept-Language"].ToString();
                var firstLang = header.Split(',')[0].Split(';')[0];
                lang = firstLang.Contains('-') ? firstLang.Split('-')[0] : firstLang;
            }

            // ─────────────────────────────────────────────────────────
            // V2.5 Path: Use Agentic Service if available
            // ─────────────────────────────────────────────────────────
            if (_agentService != null)
            {
                var chatResult = await _agentService.ChatAsync(request, lang);

                return Ok(new
                {
                    Question = request.Question,
                    Answer = chatResult.Answer,
                    LogCount = chatResult.LogCount,
                    ToolsUsed = chatResult.ToolsUsed,
                    Version = "V2.5"
                });
            }

            // ─────────────────────────────────────────────────────────
            // V1 Fallback: Use LogKnowledgeService
            // ─────────────────────────────────────────────────────────
            var endTime = request.EndTime ?? DateTime.UtcNow;
            var startTime = request.StartTime ?? endTime.AddHours(-24);

            var logs = await _logReader.GetLogsByTimeRangeAsync(startTime, endTime);

            if (logs.Count == 0)
            {
                return Ok(new { Answer = "No logs found." });
            }

            var answer = await _knowledgeService.ChatWithLogsAsync(request.Question, logs, lang);

            return Ok(new
            {
                Question = request.Question,
                Answer = answer,
                LogCount = logs.Count,
                Version = "V1"
            });
        }


        // Endpoint: Get Templates (Debug/UI)
        [Authorize(Policy = "Permission:system.view_logs")]
        [HttpGet("templates/{correlationId}")]
        public async Task<IActionResult> GetTemplates(string correlationId)
        {
            if (_logParser == null) return BadRequest("Log Parsing is not enabled.");

            var logs = await _logReader.GetLogsByCorrelationIdAsync(correlationId);
            var result = await _logParser.ParseLogsAsync(logs);

            return Ok(result.Templates);
        }

        // ═══════════════════════════════════════════════════════════════════
        // V3.0 AGENTIC ENDPOINTS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// V3.0 Agentic Investigation - Full orchestrated workflow.
        /// Uses Plan → Execute → Verify → Reflect loop.
        /// </summary>
        [Authorize(Policy = "Permission:system.view_logs")]
        [HttpPost("v3/investigate")]
        public async Task<IActionResult> InvestigateV3([FromBody] V3InvestigateRequest request)
        {
            if (_auditAgentV3 == null)
            {
                return BadRequest(new { Error = "V3.0 Agent is not enabled. Set AuditAI:Version to V3.0" });
            }

            var result = await _auditAgentV3.InvestigateAsync(
                request.Query,
                request.CorrelationId,
                HttpContext.RequestAborted
            );

            return Ok(new V3InvestigateResponse
            {
                Query = request.Query,
                Answer = result.Answer,
                Plan = result.Plan,
                ExecutionResults = result.ExecutionResults,
                VerificationPassed = result.VerificationPassed,
                Iterations = result.Iterations,
                Confidence = result.Confidence,
                Error = result.Error,
                Version = "V3.0"
            });
        }

        /// <summary>
        /// V3.0 Get Agent Trace - For debugging agent execution.
        /// </summary>
        [Authorize(Policy = "Permission:system.view_logs")]
        [HttpGet("v3/trace")]
        public IActionResult GetAgentTrace()
        {
            if (_agentObserver is Application.AI.V3.Observability.AgentTracer tracer)
            {
                return Ok(new
                {
                    Events = tracer.Events,
                    Summary = tracer.GetTraceSummary()
                });
            }

            return Ok(new { Message = "Agent tracing not available" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // REQUEST / RESPONSE DTOs
    // ═══════════════════════════════════════════════════════════════════

    public class V3InvestigateRequest
    {
        /// <summary>Investigation query/question</summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>Optional correlation ID to focus investigation</summary>
        public string? CorrelationId { get; set; }
    }

    public class V3InvestigateResponse
    {
        public string Query { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public List<string> Plan { get; set; } = new();
        public List<string> ExecutionResults { get; set; } = new();
        public bool VerificationPassed { get; set; }
        public int Iterations { get; set; }
        public float Confidence { get; set; }
        public string? Error { get; set; }
        public string Version { get; set; } = "V3.0";
    }

    public class ChatRequest
    {
        public string Question { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? CorrelationId { get; set; }
    }
}
