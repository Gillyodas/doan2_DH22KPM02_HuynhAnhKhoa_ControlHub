using ControlHub.Application.AI;
using ControlHub.Application.AI.V3;
using ControlHub.Application.AI.V3.Agentic;
using ControlHub.Application.AI.V3.Observability;
using ControlHub.Application.AI.V3.Parsing;
using ControlHub.Application.AI.V3.RAG;
using ControlHub.Application.AI.V3.Reasoning;
using ControlHub.Application.AI.V3.Resilience;
using ControlHub.Application.Common.Interfaces.AI;
using ControlHub.Application.Common.Interfaces.AI.V1;
using ControlHub.Application.Common.Interfaces.AI.V3;
using ControlHub.Application.Common.Interfaces.AI.V3.Agentic;
using ControlHub.Application.Common.Interfaces.AI.V3.Observability;
using ControlHub.Application.Common.Interfaces.AI.V3.Parsing;
using ControlHub.Application.Common.Interfaces.AI.V3.RAG;
using ControlHub.Application.Common.Interfaces.AI.V3.Reasoning;
using ControlHub.Application.Common.Interfaces.AI.V3.Resilience;
using ControlHub.Infrastructure.AI;
using ControlHub.Infrastructure.AI.Parsing;
using ControlHub.Infrastructure.AI.Strategies;
using ControlHub.Infrastructure.AI.V3;
using ControlHub.Infrastructure.AI.V3.ML;
using ControlHub.Infrastructure.AI.V3.RAG;
using ControlHub.Infrastructure.AI.V3.Reasoning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControlHub.Infrastructure.DependencyInjection;

internal static class AiExtensions
{
    internal static IServiceCollection AddControlHubAi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // HTTP Clients
        services.AddHttpClient<IVectorDatabase, QdrantVectorStore>(
            c => c.Timeout = TimeSpan.FromMinutes(3));
        services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(
            c => c.Timeout = TimeSpan.FromMinutes(2));
        services.AddHttpClient<IAIAnalysisService, LocalAIAdapter>(
            c => c.Timeout = TimeSpan.FromMinutes(3));

        // Core AI (shared across versions)
        services.AddScoped<ILogParserService, Drain3ParserService>();
        services.AddScoped<IRunbookService, RunbookService>();
        services.AddScoped<ILogKnowledgeService, LogKnowledgeService>();
        services.AddScoped<LogKnowledgeService>();

        // Sampling Strategy
        var samplingStrategy = configuration["AuditAI:SamplingStrategy"] ?? "Naive";
        if (samplingStrategy == "WeightedReservoir")
            services.AddScoped<ISamplingStrategy, WeightedReservoirSamplingStrategy>();
        else
            services.AddScoped<ISamplingStrategy, NaiveSamplingStrategy>();

        // Version routing
        var aiVersion = configuration["AuditAI:Version"] ?? "V1";
        return aiVersion switch
        {
            "V3.0" => services.AddAiV3(),
            "V2.5" => services.AddAiV25(),
            _ => services  // V1: core services đã đủ
        };
    }

    private static IServiceCollection AddAiV25(this IServiceCollection services)
    {
        services.AddScoped<IAuditAgentService, AgenticAuditService>();
        return services;
    }

    private static IServiceCollection AddAiV3(this IServiceCollection services)
    {
        // Phase 1: Hybrid Parsing
        services.AddSingleton<ISemanticLogClassifier, OnnxLogClassifier>();
        services.AddScoped<IHybridLogParser, HybridLogParser>();

        // Phase 2: Enhanced RAG
        services.AddSingleton<IReranker, OnnxReranker>();
        services.AddScoped<IMultiHopRetriever, MultiHopRetriever>();
        services.AddScoped<IAgenticRAG, AgenticRAGService>();
        services.AddScoped<ILogEvidenceProcessor, LogEvidenceProcessor>();
        services.AddSingleton<ISystemKnowledgeProvider, SystemKnowledgeProvider>();

        // Phase 3: Reasoning
        services.AddHttpClient<IReasoningModel, ReasoningModelClient>(
            c => c.Timeout = TimeSpan.FromMinutes(5));
        services.AddScoped<IConfidenceScorer, ConfidenceScorer>();

        // Phase 4: Agentic Orchestration
        services.AddScoped<IStateGraph, StateGraph>();
        services.AddScoped<IToolRegistry, ToolRegistry>();
        services.AddScoped<IAuditAgentV3, AuditAgentV3>();

        // Phase 5: Production Hardening
        services.AddScoped<IAgentObserver, AgentTracer>();
        services.AddSingleton<ICircuitBreaker, CircuitBreaker>();
        services.AddScoped<IFallbackStrategy, GracefulDegradation>();

        services.AddScoped<IAuditAgentService, AgenticAuditServiceV3>();
        return services;
    }
}