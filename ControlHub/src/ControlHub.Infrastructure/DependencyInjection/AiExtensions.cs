using ControlHub.Application.AuditAI.Interfaces;
using ControlHub.Application.AuditAI.Interfaces.V1;
using ControlHub.Application.AuditAI.Interfaces.V3;
using ControlHub.Application.AuditAI.Interfaces.V3.Agentic;
using ControlHub.Application.AuditAI.Interfaces.V3.Observability;
using ControlHub.Application.AuditAI.Interfaces.V3.Parsing;
using ControlHub.Application.AuditAI.Interfaces.V3.RAG;
using ControlHub.Application.AuditAI.Interfaces.V3.Reasoning;
using ControlHub.Application.AuditAI.Interfaces.V3.Resilience;
using ControlHub.Infrastructure.AI;
using ControlHub.Infrastructure.AI.Parsing;
using ControlHub.Infrastructure.AI.Strategies;
using ControlHub.Infrastructure.AI.V3;
using ControlHub.Infrastructure.AI.V3.Agentic;
using ControlHub.Infrastructure.AI.V3.ML;
using ControlHub.Infrastructure.AI.V3.Observability;
using ControlHub.Infrastructure.AI.V3.RAG;
using ControlHub.Infrastructure.AI.V3.Reasoning;
using AgenticRAGServiceImpl = ControlHub.Infrastructure.AI.V3.RAG.AgenticRAGService;
using MultiHopRetrieverImpl = ControlHub.Infrastructure.AI.V3.RAG.MultiHopRetriever;
using ConfidenceScorerImpl = ControlHub.Infrastructure.AI.V3.Reasoning.ConfidenceScorer;
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
            _ => services  // V1/V2.5: core services sufficient
        };
    }

    private static IServiceCollection AddAiV3(this IServiceCollection services)
    {
        // Phase 1: Hybrid Parsing
        services.AddSingleton<ISemanticLogClassifier, OnnxLogClassifier>();

        // Phase 2: Enhanced RAG
        services.AddSingleton<IReranker, OnnxReranker>();
        services.AddScoped<ILogEvidenceProcessor, LogEvidenceProcessor>();
        services.AddSingleton<ISystemKnowledgeProvider, SystemKnowledgeProvider>();

        // Phase 3: Reasoning
        services.AddHttpClient<IReasoningModel, ReasoningModelClient>(
            c => c.Timeout = TimeSpan.FromMinutes(5));

        // Phase 3b: RAG pipeline
        services.AddScoped<IMultiHopRetriever, MultiHopRetrieverImpl>();
        services.AddScoped<IAgenticRAG, AgenticRAGServiceImpl>();
        services.AddScoped<IConfidenceScorer, ConfidenceScorerImpl>();

        // Phase 4: Agentic Orchestration
        services.AddScoped<IAgentObserver, AgentTracer>();
        services.AddScoped<IStateGraph, StateGraph>();
        services.AddScoped<IAuditAgentV3, AuditAgentV3>();

        return services;
    }
}
