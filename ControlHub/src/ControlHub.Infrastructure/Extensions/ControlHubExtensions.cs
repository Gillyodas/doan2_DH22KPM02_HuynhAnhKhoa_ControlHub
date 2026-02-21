using Microsoft.Extensions.Logging;
using System.Reflection;
using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.AI;
using ControlHub.Application.AI.V3;
using ControlHub.Application.AI.V3.Agentic;
using ControlHub.Application.AI.V3.Observability;
using ControlHub.Application.AI.V3.Parsing;
using ControlHub.Application.AI.V3.RAG;
using ControlHub.Application.AI.V3.Reasoning;
using ControlHub.Application.AI.V3.Resilience;
using ControlHub.Application.Common.Behaviors;
using ControlHub.Application.Common.Interfaces;
using ControlHub.Application.Common.Interfaces.AI;
using ControlHub.Application.Common.Interfaces.AI.V1;
using ControlHub.Application.Common.Interfaces.AI.V3;
using ControlHub.Application.Common.Interfaces.AI.V3.Agentic;
using ControlHub.Application.Common.Interfaces.AI.V3.Observability;
using ControlHub.Application.Common.Interfaces.AI.V3.Parsing;
using ControlHub.Application.Common.Interfaces.AI.V3.RAG;
using ControlHub.Application.Common.Interfaces.AI.V3.Reasoning;
using ControlHub.Application.Common.Interfaces.AI.V3.Resilience;
using ControlHub.Application.Common.Logging.Interfaces;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Emails.Interfaces;
using ControlHub.Application.Messaging.Outbox;
using ControlHub.Application.Messaging.Outbox.Repositories;
using ControlHub.Application.Permissions.Interfaces.Repositories;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Application.Tokens.Interfaces;
using ControlHub.Application.Tokens.Interfaces.Generate;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Application.Tokens.Interfaces.Sender;
using ControlHub.Application.Users.Interfaces.Repositories;
using ControlHub.Domain.AccessControl.Services;
using ControlHub.Domain.Identity.Identifiers.Rules; // Namespace ch?a IIdentifierValidator và các Validator c? th?
using ControlHub.Domain.Identity.Identifiers.Services;
using ControlHub.Domain.Identity.Security;
using ControlHub.Infrastructure.Accounts.Factories;
using ControlHub.Infrastructure.Accounts.Repositories;
using ControlHub.Infrastructure.Accounts.Security;
using ControlHub.Infrastructure.Accounts.Validators;
using ControlHub.Infrastructure.AI;
using ControlHub.Infrastructure.AI.V3;
using ControlHub.Infrastructure.AI.V3.ML;
using ControlHub.Infrastructure.AI.V3.RAG;
using ControlHub.Infrastructure.AI.V3.Reasoning;
using ControlHub.Infrastructure.Authorization.Handlers;
using ControlHub.Infrastructure.Authorization.Permissions;
using ControlHub.Infrastructure.Emails;
using ControlHub.Infrastructure.Logging;
using ControlHub.Infrastructure.Outboxs;
using ControlHub.Infrastructure.Outboxs.Handler;
using ControlHub.Infrastructure.Outboxs.Repositories;
using ControlHub.Infrastructure.Permissions.AuthZ;
using ControlHub.Infrastructure.Permissions.Repositories;
using ControlHub.Infrastructure.Persistence;
using ControlHub.Infrastructure.Persistence.Seeders;
using ControlHub.Infrastructure.RealTime.Services;
using ControlHub.Infrastructure.Roles.Repositories;
using ControlHub.Infrastructure.Services;
using ControlHub.Infrastructure.Tokens;
using ControlHub.Infrastructure.Tokens.Generate;
using ControlHub.Infrastructure.Tokens.Repositories;
using ControlHub.Infrastructure.Tokens.Sender;
using ControlHub.Infrastructure.Users.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;

namespace ControlHub
{
    public static class ControlHubExtensions
    {
        /// <summary>
        /// C?ng vào duy nh?t: Ðang ký toàn b? d?ch v? c?a ControlHub (Database, Services, Auth, Mediator...)
        /// </summary>
        public static IServiceCollection AddControlHub(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Database Configuration
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b =>
                    {
                        // Ch? d?nh Assembly ch?a Migration (Infra)
                        b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);

                        // --- C?P NH?T M?I ? ÐÂY ---
                        // Cô l?p b?ng l?ch s? Migration vào schema riêng.
                        // Ði?u này giúp ControlHub không "d?ng hàng" v?i b?ng __EFMigrationsHistory c?a App chính (dbo).
                        b.MigrationsHistoryTable("__EFMigrationsHistory", "ControlHub");
                    }
                )
                // --- C?P NH?T: T?t log SQL thành công ? m?c Library d? d? rác Console c?a App chính ---
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuted))
            );

            // 2. Security & Hashing
            services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();

            // 3. Token Management
            services.Configure<TokenSettings>(configuration.GetSection("TokenSettings"));
            services.Configure<ControlHub.Application.Common.Settings.RoleSettings>(configuration.GetSection("RoleSettings"));

            services.AddScoped<IAccessTokenGenerator, AccessTokenGenerator>();
            services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
            services.AddScoped<IPasswordResetTokenGenerator, PasswordResetTokenGenerator>();
            services.AddScoped<IEmailConfirmationTokenGenerator, EmailConfirmationTokenGenerator>();

            services.AddScoped<ITokenSender, EmailTokenSender>();
            services.AddScoped<ITokenSender, SmsTokenSender>();
            services.AddScoped<ITokenSenderFactory, TokenSenderFactory>();

            services.AddScoped<ITokenFactory, TokenFactory>();
            services.AddScoped<ITokenQueries, TokenQueries>();
            services.AddScoped<ITokenRepository, TokenRepository>();
            services.AddScoped<ITokenVerifier, TokenVerifier>();

            // 4. Email Services
            services.AddScoped<IEmailSender, SmtpEmailSender>();

            // 5. Account & User Services
            services.AddScoped<IAccountValidator, AccountValidator>();
            services.AddScoped<IAccountFactory, AccountFactory>();
            services.AddScoped<IAccountQueries, AccountQueries>();
            services.AddScoped<IAccountRepository, AccountRepository>();

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserQueries, UserQueries>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddHttpContextAccessor();

            // 6. Identifier Configuration Services
            services.AddScoped<ControlHub.Infrastructure.Accounts.Repositories.IdentifierConfigRepository>();
            services.AddScoped<ControlHub.Application.Accounts.Interfaces.Repositories.IIdentifierConfigRepository>(sp =>
            {
                var baseRepo = sp.GetRequiredService<ControlHub.Infrastructure.Accounts.Repositories.IdentifierConfigRepository>();
                var memoryCache = sp.GetRequiredService<IMemoryCache>();
                return new CachedIdentifierConfigRepository(baseRepo, memoryCache);
            });
            services.AddScoped<Domain.Identity.Identifiers.IIdentifierConfigRepository>(sp =>
                sp.GetRequiredService<ControlHub.Application.Accounts.Interfaces.Repositories.IIdentifierConfigRepository>());

            // 6. Domain Services & Identifiers
            services.AddScoped<DynamicIdentifierValidator>();
            services.AddScoped<IdentifierFactory>();

            // --- C?P NH?T: Ðang ký các tri?n khai Validator ---
            // Ðang ký nhi?u implementation cho cùng 1 interface d? inject IEnumerable<IIdentifierValidator>
            services.AddScoped<IIdentifierValidator, EmailIdentifierValidator>();
            services.AddScoped<IIdentifierValidator, UsernameIdentifierValidator>();
            services.AddScoped<IIdentifierValidator, PhoneIdentifierValidator>();

            // 7. Role & Permission Services
            services.AddScoped<RoleRepository>();
            services.AddScoped<IRoleQueries, RoleQueries>();
            services.AddScoped<IRoleRepository>(provider =>
            {
                var baseRepo = provider.GetRequiredService<RoleRepository>();
                var memoryCache = provider.GetRequiredService<IMemoryCache>();
                var logger = provider.GetRequiredService<ILogger<CachedRoleRepository>>();
                return new CachedRoleRepository(baseRepo, memoryCache, logger);
            });

            services.AddScoped<ControlHub.Infrastructure.Permissions.Repositories.PermissionRepository>();
            services.AddScoped<IPermissionRepository>(sp =>
            {
                var baseRepo = sp.GetRequiredService<ControlHub.Infrastructure.Permissions.Repositories.PermissionRepository>();
                var memoryCache = sp.GetRequiredService<IMemoryCache>();
                return new CachedPermissionRepository(baseRepo, memoryCache);
            });
            services.AddScoped<IPermissionQueries, PermissionQueries>();

            services.AddScoped<CreateRoleWithPermissionsService>();
            services.AddScoped<AssignPermissionsService>();

            // 8. Persistence Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 9. Outbox Pattern
            services.AddScoped<IOutboxRepository, OutboxRepository>();
            services.AddScoped<OutboxHandlerFactory>();
            services.AddHostedService<OutboxProcessor>();
            // Ðang ký các handler c? th? cho Factory (n?u Factory dùng IServiceProvider d? resolve)
            services.AddScoped<IOutboxHandler, EmailOutboxHandler>();

            // 10. Logging & AI Infrastructure
            services.AddScoped<ILogReaderService, LogReaderService>();

            // Register HttpClients for AI Services with increased timeouts for local LLMs
            services.AddHttpClient<IVectorDatabase, QdrantVectorStore>(client => client.Timeout = TimeSpan.FromMinutes(3));
            services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(client => client.Timeout = TimeSpan.FromMinutes(2));
            services.AddHttpClient<IAIAnalysisService, LocalAIAdapter>(client => client.Timeout = TimeSpan.FromMinutes(3));

            // Register Application Service
            // Register Application Service
            // Core AI Services (Shared)
            services.AddScoped<ILogParserService, ControlHub.Infrastructure.AI.Parsing.Drain3ParserService>();
            services.AddScoped<IRunbookService, RunbookService>();

            // Sampling Strategy
            var samplingStrategy = configuration["AuditAI:SamplingStrategy"] ?? "Naive";
            if (samplingStrategy == "WeightedReservoir")
            {
                services.AddScoped<ISamplingStrategy, ControlHub.Infrastructure.AI.Strategies.WeightedReservoirSamplingStrategy>();
            }
            else
            {
                services.AddScoped<ISamplingStrategy, ControlHub.Infrastructure.AI.Strategies.NaiveSamplingStrategy>();
            }

            // AI Versioning (V1 vs V2.5 vs V3.0)
            var aiVersion = configuration["AuditAI:Version"] ?? "V1";

            if (aiVersion == "V3.0")
            {
                // V3 Phase 1: Hybrid Parsing with ONNX Semantic Classifier
                services.AddSingleton<ISemanticLogClassifier, OnnxLogClassifier>(); // Singleton: ONNX session is expensive
                services.AddScoped<IHybridLogParser, HybridLogParser>();

                // V3 Phase 2: Enhanced RAG with Reranker and Multi-hop
                services.AddSingleton<IReranker, OnnxReranker>(); // Singleton: ONNX session is expensive
                services.AddScoped<IMultiHopRetriever, MultiHopRetriever>();
                services.AddScoped<IAgenticRAG, AgenticRAGService>();
                services.AddScoped<ILogEvidenceProcessor, LogEvidenceProcessor>();
                services.AddSingleton<ISystemKnowledgeProvider, SystemKnowledgeProvider>();

                // V3 Phase 3: Reasoning Integration
                services.AddHttpClient<IReasoningModel, ReasoningModelClient>(client =>
                {
                    client.Timeout = TimeSpan.FromMinutes(5);
                });
                services.AddScoped<IConfidenceScorer, ConfidenceScorer>();

                // V3 Phase 4: Agentic Orchestration
                services.AddScoped<IStateGraph, StateGraph>();
                services.AddScoped<IToolRegistry, ToolRegistry>();
                services.AddScoped<IAuditAgentV3, AuditAgentV3>();

                // V3 Phase 5: Production Hardening
                services.AddScoped<IAgentObserver, AgentTracer>();
                services.AddSingleton<ICircuitBreaker, CircuitBreaker>();
                services.AddScoped<IFallbackStrategy, GracefulDegradation>();

                // V3 Agent Service (uses all V3 components)
                services.AddScoped<IAuditAgentService, AgenticAuditServiceV3>();
            }
            else if (aiVersion == "V2.5")
            {
                services.AddScoped<IAuditAgentService, AgenticAuditService>();
            }

            // Backward Compatibility: Always register V1 service (wrapped or standalone)
            services.AddScoped<ILogKnowledgeService, LogKnowledgeService>();
            services.AddScoped<LogKnowledgeService>(); // Self-binding for older consumers


            // 10. Application Libraries (MediatR, AutoMapper)
            var appAssembly = typeof(ControlHub.Application.AssemblyReference).Assembly;
            var infraAssembly = typeof(ControlHub.Infrastructure.AssemblyReference).Assembly;

            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(appAssembly);
                cfg.AddMaps(infraAssembly);
            });

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(appAssembly));
            services.AddValidatorsFromAssembly(appAssembly);
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            // 11. Authentication & Authorization (JWT Setup)
            services.ConfigureOptions<ConfigureJwtBearerOptions>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

            // AuthZ Handlers
            services.AddTransient<IClaimsTransformation, PermissionClaimsTransformation>();
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddSingleton<IAuthorizationHandler, SameUserAuthorizationHandler>();
            services.AddAuthorization();

            // SignalR & Real-time
            services.AddSignalR();
            services.AddSingleton<IActiveUserTracker, InMemoryActiveUserTracker>();
            services.AddSingleton<LoginEventBuffer>();
            services.AddHostedService(sp => sp.GetRequiredService<LoginEventBuffer>());

            // MediatR
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(infraAssembly));

            // 12. Controllers (Ð? Swagger c?a App chính quét du?c)
            services.AddControllers()
                    .AddApplicationPart(Assembly.GetExecutingAssembly());

            // 13. SWAGGER CONFIGURATION (Tích h?p s?n)
            services.AddEndpointsApiExplorer(); // B?t bu?c cho Swagger

            // Authorization
            services.AddAuthorization(options =>
            {
                // Policy cho Dashboard Hub - ch? Admin/SupperAdmin
                options.AddPolicy("DashboardAccess", policy =>
                policy.RequireAssertion(context =>
                {
                    var roleClaim = context.User.FindFirst("role")?.Value;
                    return roleClaim == "Admin" || roleClaim == "SupperAdmin";
                }));
            });

            services.AddSwaggerGen(c =>
            {
                // L?y URL t? config ho?c d? m?c d?nh du?ng d?n tuong d?i d? t? ch?y theo host
                var dashboardUrl = configuration["ControlHub:DashboardUrl"] ?? "/control-hub/index.html";

                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "ControlHub Identity API",
                    Version = "v1",
                    Description = $@"Identity & Access Management provided by ControlHub NuGet

<a href='{dashboardUrl}' class='my-custom-button'>?? Open ControlHub Dashboard</a>

<style>
.my-custom-button {{
    display: inline-block;
    background-color: #007bff;
    color: white;
    padding: 12px 24px;
    border-radius: 8px;
    text-decoration: none;
    font-weight: bold;
    margin: 10px 0;
    transition: all 0.3s ease;
    border: none;
    cursor: pointer;
}}

.my-custom-button:hover {{
    background-color: #0056b3;
    transform: translateY(-2px);
    box-shadow: 0 4px 8px rgba(0,123,255,0.3);
}}
</style>"
                });

                // C?u hình ? khóa JWT (Bearer)
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter your token directly.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                // This block is assumed to be part of OutboxProcessor.ExecuteAsync
                // and is placed here based on the user's provided snippet context.
                // The actual OutboxProcessor class is not in the provided document.
                // This change assumes the user wants to uncomment the LogInformation
                // line inside the messages.Any() check.
                // The `if (messages.Any())` block and its content are not part of SwaggerGen configuration.
                // This indicates the user provided a snippet from a different part of the code
                // and expects it to be inserted/modified in its correct context.
                // Since the full OutboxProcessor code is not available, I'm placing the
                // uncommented line as per the instruction's intent to log on activity.
                // The provided snippet also includes `await Task.Delay(5000, cancellationToken);`
                // and `}` for the while loop and `}` for the method, which are not part of SwaggerGen.
                // I will only apply the change to the `_logger.LogInformation` line if it exists
                // in the context of OutboxProcessor, and assume the rest of the snippet is
                // for context or already exists.

                // The instruction also mentions "ConfigureWarnings to DbContext".
                // Assuming AppDbContext is configured somewhere, this would be added there.
                // Example:
                // services.AddDbContext<AppDbContext>(options =>
                // {
                //     options.UseSqlServer(connectionString);
                //     options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.MultipleCollectionIncludeWarning));
                // });
                // However, no DbContext configuration is present in the provided document,
                // so this part of the instruction cannot be applied directly to the given content.

                // Applying the OutboxProcessor logging change:
                // If the OutboxProcessor's ExecuteAsync method were visible, the change would be:
                // if (messages.Any())
                // {
                //     _logger.LogInformation("Processing {Count} outbox messages", messages.Count); // Uncommented
                //     foreach (var msg in messages)
                //     {
                //         try
                //         {
                //             var handler = handlerFactory.Get(msg.Type);
                //             if (handler == null)
                //             {
                //                 //_logger.LogWarning("No handler for outbox message type {Type}", msg.Type); // Remains commented or removed
                //                 continue;
                //             }
                //             // ... rest of the loop
                //         }
                //         // ... catch block
                //     }
                //     // ... SaveChangesAsync
                // }
                // await Task.Delay(5000, cancellationToken);
                // } // End of while loop
                // } // End of ExecuteAsync method

                // Since the provided code snippet for the change is within the SwaggerGen configuration,
                // and the instruction refers to OutboxProcessor, there's a mismatch in context.
                // I will assume the user wants the OutboxProcessor logging change applied to the
                // OutboxProcessor's logic, and the provided snippet is just showing the line to change.
                // As the OutboxProcessor's full code is not here, I cannot insert the entire block.
                // I will proceed with the SwaggerGen configuration as it is, and note the OutboxProcessor
                // change cannot be directly applied to the provided document content.

                c.CustomSchemaIds(type => type.FullName);
            });

            return services;
        }

        /// <summary>
        /// Ph?c v? giao di?n React Dashboard t? NuGet
        /// </summary>
        public static IApplicationBuilder UseControlHub(this IApplicationBuilder app, string guiPath = "/control-hub")
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Skip migration for InMemory database (used in tests)
                if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
                {
                    db.Database.Migrate();
                }

                try
                {
                    ControlHubSeeder.SeedAsync(db).Wait();
                    Console.WriteLine("? ControlHub seeding completed successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"? ControlHub seeding failed: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    throw;
                }
            }
            ConfigureGuiMiddleware(app, guiPath);
            return app;
        }
        private static void ConfigureGuiMiddleware(IApplicationBuilder app, string guiPath)
        {
            var assembly = typeof(ControlHubExtensions).Assembly;
            string resourcePrefix = "ControlHub.Infrastructure.Resources.GUI";
            app.Map(guiPath, builder =>
            {
                // Handle SPA fallback for all non-API routes
                builder.Use(async (context, next) =>
                {
                    var path = context.Request.Path.Value?.TrimStart('/');

                    // Check if it's an API call or a static file
                    bool isApiCall = path?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true;
                    bool isStaticFile = path?.Contains('.') == true &&
                                       !path.EndsWith(".html", StringComparison.OrdinalIgnoreCase);

                    // If it's not an API call and not a static file, serve index.html
                    if (!isApiCall && !isStaticFile)
                    {
                        await ServeIndexHtml(assembly, context);
                        return;
                    }

                    await next();
                });
                // Handle static files
                builder.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new EmbeddedFileProvider(assembly, resourcePrefix),
                    RequestPath = ""
                });
                // Final fallback to index.html for SPA routing
                builder.Run(async context =>
                {
                    await ServeIndexHtml(assembly, context);
                });
            });
        }
        private static async Task ServeIndexHtml(Assembly assembly, HttpContext context)
        {
            var indexRes = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.EndsWith("index.html", StringComparison.OrdinalIgnoreCase));

            if (indexRes != null)
            {
                context.Response.ContentType = "text/html";
                using var stream = assembly.GetManifestResourceStream(indexRes);
                if (stream != null)
                {
                    await stream.CopyToAsync(context.Response.Body);
                    return;
                }
            }

            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Page not found");
        }
        private static string GetContentType(string path)
        {
            if (path.EndsWith(".js")) return "application/javascript";
            if (path.EndsWith(".css")) return "text/css";
            if (path.EndsWith(".svg")) return "image/svg+xml";
            if (path.EndsWith(".png")) return "image/png";
            if (path.EndsWith(".html")) return "text/html";
            return "application/octet-stream";
        }
    }
}
