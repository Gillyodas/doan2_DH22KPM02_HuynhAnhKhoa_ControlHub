# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**ControlHub** is a .NET 8 Identity & Access Management (IAM) system designed to be distributed as a NuGet package. It provides complete AuthN/AuthZ, AI-powered audit/log analysis, and an embedded React dashboard. The primary solution is in `ControlHub/`.

## Commands

All commands should be run from `ControlHub/` unless noted otherwise.

### Build & Run
```bash
# Build the solution
dotnet build ControlHub.sln

# Run the API
dotnet run --project src/ControlHub.API

# Build the React UI (must be done before building Infrastructure to embed it)
cd src/ControlHub.UI && npm install && npm run build
```

### Testing
```bash
# Run all tests
dotnet test ControlHub.sln

# Run a specific test project
dotnet test tests/ControlHub.Api.Tests

# Run a single test class
dotnet test tests/ControlHub.Api.Tests --filter "FullyQualifiedName~LoginTests"
```

### Database Migrations
Migrations target `ControlHub.Infrastructure` (DbContext is defined there):
```bash
# Add migration (run from ControlHub/ directory)
dotnet ef migrations add <MigrationName> --project src/ControlHub.Infrastructure --startup-project src/ControlHub.API

# Migrations auto-apply on startup via UseControlHub() middleware
```

### Infrastructure Services (Docker)
```bash
# Start Qdrant (vector DB) and Ollama (LLM)
docker-compose up -d
```

### UI Development
```bash
cd src/ControlHub.UI
npm run dev      # Dev server on port 3000
npm run lint     # ESLint
npm run format   # Prettier
```

## Architecture

The solution follows Clean Architecture with DDD + CQRS:

```
ControlHub/
├── src/
│   ├── ControlHub.Domain          # Entities, aggregates, value objects, domain events
│   ├── ControlHub.Application     # CQRS handlers (MediatR), interfaces, behaviors
│   ├── ControlHub.Infrastructure  # EF Core, repos, external services implementations
│   ├── ControlHub.SharedKernel    # Shared types: Result<T>, errors, common DTOs
│   ├── ControlHub.API             # ASP.NET Core host; thin controllers
│   └── ControlHub.UI              # React + TypeScript + Vite embedded dashboard
└── tests/
    ├── ControlHub.Api.Tests        # Functional/integration tests (WebApplicationFactory)
    ├── ControlHub.Application.Tests
    ├── ControlHub.Domain.Tests
    └── ControlHub.Infrastructure.Tests
```

### Key Architectural Decisions

**Library Entry Point**: `ControlHubExtensions` (in `src/ControlHub.Infrastructure/Extensions/ControlHubExtensions.cs`) exposes two public methods consumed by host applications:
- `AddControlHub(configuration)` — registers all services
- `UseControlHub()` — runs EF migrations, seeds data, and configures embedded React UI middleware

**Embedded React UI**: The `ControlHub.UI/dist/` build output is embedded as resources in `ControlHub.Infrastructure` and served at `/control-hub`. The `.csproj` uses `<EmbeddedResource>` to bundle the Vite build. **Always rebuild the UI before building Infrastructure if UI changes are made.**

**Domain Model**: Core aggregate is `Account` (in `Domain/Identity/Aggregates/`) which owns `Identifier` collection (email/phone/username/custom), `Token` collection, and references `Role` → `Permission` graph.

**CQRS via MediatR**: All business logic goes through MediatR commands/queries in `Application`. `ValidationBehavior` pipeline behavior auto-validates with FluentValidation. Internal visibility is granted to Infrastructure and API via `InternalsVisibleTo`.

**Rate Limiting**: Configured separately via `AddControlHubRateLimiting()` and `UseRateLimiter()` in the host app.

**Dependency Injection**: `Infrastructure/DependencyInjection/` contains one extension per domain area (e.g., `AccountExtensions`, `SecurityExtensions`, `AiExtensions`, etc.), all aggregated into `AddControlHub()`.

### AuditAI Module (V3.0)

The AI log analysis pipeline lives in `Application/Common/Interfaces/AI/V3/` (interfaces) and `Infrastructure/AI/V3/` (implementations):

- **Parsing**: `OnnxLogClassifier` uses a BERT-based ONNX model for semantic log parsing; falls back to Drain3 for structured logs
- **RAG**: `OnnxReranker` reranks retrieved runbooks; `QdrantVectorStore` handles vector search
- **Reasoning**: `ReasoningModelClient` calls Ollama with Chain-of-Thought prompts
- **Sampling**: `WeightedReservoirSamplingStrategy` prioritizes Error/Fatal logs

Config in `appsettings.json` under `AuditAI.V3` — ONNX model paths must be absolute.

### Configuration Requirements

Key `appsettings.json` sections:
- `ConnectionStrings:DefaultConnection` — SQL Server
- `Jwt` — Issuer, Audience, Key
- `AI.OllamaUrl` / `AI.ModelName` — Ollama endpoint
- `AuditAI.V3.Parsing.OnnxModelPath` / `.VocabPath` — BERT ONNX model files
- `RoleSettings` — fixed GUIDs for SuperAdmin/Admin/User roles (used in seeding)

### SDK Version
.NET SDK 8.0.416 (pinned via `global.json`).
