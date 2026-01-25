using System.Reflection;
using ControlHub.Application.AI;
using ControlHub.Application.Common.Interfaces.AI;
using ControlHub.Application.Common.Logging.Interfaces;
using ControlHub.Infrastructure.AI;
using ControlHub.Infrastructure.Logging;
using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Behaviors;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Emails.Interfaces;
using ControlHub.Application.OutBoxs;
using ControlHub.Application.OutBoxs.Repositories;
using ControlHub.Application.Permissions.Interfaces.Repositories;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Application.Tokens.Interfaces;
using ControlHub.Application.Tokens.Interfaces.Generate;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Application.Tokens.Interfaces.Sender;
using ControlHub.Application.Users.Interfaces.Repositories;
using ControlHub.Domain.Accounts.Identifiers.Rules; // Namespace chứa IIdentifierValidator và các Validator cụ thể
using ControlHub.Domain.Accounts.Identifiers.Services;
using ControlHub.Domain.Accounts.Security;
using ControlHub.Domain.Common.Services;
using ControlHub.Infrastructure.Accounts.Factories;
using ControlHub.Infrastructure.Accounts.Repositories;
using ControlHub.Infrastructure.Accounts.Security;
using ControlHub.Infrastructure.Accounts.Validators;
using ControlHub.Infrastructure.Authorization.Handlers;
using ControlHub.Infrastructure.Authorization.Permissions;
using ControlHub.Infrastructure.Emails;
using ControlHub.Infrastructure.Outboxs;
using ControlHub.Infrastructure.Outboxs.Handler;
using ControlHub.Infrastructure.Outboxs.Repositories;
using ControlHub.Infrastructure.Permissions.AuthZ;
using ControlHub.Infrastructure.Permissions.Repositories;
using ControlHub.Infrastructure.Persistence;
using ControlHub.Infrastructure.Persistence.Seeders;
using ControlHub.Infrastructure.Roles.Repositories;
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
        /// Cổng vào duy nhất: Đăng ký toàn bộ dịch vụ của ControlHub (Database, Services, Auth, Mediator...)
        /// </summary>
        public static IServiceCollection AddControlHub(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Database Configuration
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b =>
                    {
                        // Chỉ định Assembly chứa Migration (Infra)
                        b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);

                        // --- CẬP NHẬT MỚI Ở ĐÂY ---
                        // Cô lập bảng lịch sử Migration vào schema riêng.
                        // Điều này giúp ControlHub không "đụng hàng" với bảng __EFMigrationsHistory của App chính (dbo).
                        b.MigrationsHistoryTable("__EFMigrationsHistory", "ControlHub");
                    }
                ));

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

            // 6. Identifier Configuration Services
            services.AddScoped<ControlHub.Infrastructure.Accounts.Repositories.IdentifierConfigRepository>();
            services.AddScoped<ControlHub.Application.Accounts.Interfaces.Repositories.IIdentifierConfigRepository>(sp =>
            {
                var baseRepo = sp.GetRequiredService<ControlHub.Infrastructure.Accounts.Repositories.IdentifierConfigRepository>();
                var memoryCache = sp.GetRequiredService<IMemoryCache>();
                return new CachedIdentifierConfigRepository(baseRepo, memoryCache);
            });
            services.AddScoped<Domain.Accounts.Identifiers.IIdentifierConfigRepository>(sp => 
                sp.GetRequiredService<ControlHub.Application.Accounts.Interfaces.Repositories.IIdentifierConfigRepository>());

            // 6. Domain Services & Identifiers
            services.AddScoped<DynamicIdentifierValidator>();
            services.AddScoped<IdentifierFactory>();

            // --- CẬP NHẬT: Đăng ký các triển khai Validator ---
            // Đăng ký nhiều implementation cho cùng 1 interface để inject IEnumerable<IIdentifierValidator>
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

                return new CachedRoleRepository(baseRepo, memoryCache);
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
            // Đăng ký các handler cụ thể cho Factory (nếu Factory dùng IServiceProvider để resolve)
            services.AddScoped<IOutboxHandler, EmailOutboxHandler>();
            
            // 10. Logging & AI Infrastructure
            services.AddScoped<ILogReaderService, LogReaderService>();

            // Register HttpClients for AI Services
            services.AddHttpClient<IVectorDatabase, QdrantVectorStore>();
            services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>();
            services.AddHttpClient<IAIAnalysisService, LocalAIAdapter>();

            // Register Application Service
            services.AddScoped<LogKnowledgeService>();


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

            // 12. Controllers (Để Swagger của App chính quét được)
            services.AddControllers()
                    .AddApplicationPart(Assembly.GetExecutingAssembly());

            // 13. SWAGGER CONFIGURATION (Tích hợp sẵn)
            services.AddEndpointsApiExplorer(); // Bắt buộc cho Swagger

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "ControlHub Identity API",
                    Version = "v1",
                    Description = @"Identity & Access Management provided by ControlHub NuGet

<a href='https://localhost:7110/control-hub/index.html' class='my-custom-button'>🚀 Open ControlHub Dashboard</a>

<style>
.my-custom-button {
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
}

.my-custom-button:hover {
    background-color: #0056b3;
    transform: translateY(-2px);
    box-shadow: 0 4px 8px rgba(0,123,255,0.3);
}
</style>"
                });

                // Cấu hình ổ khóa JWT (Bearer)
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter your token directly.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }

        /// <summary>
        /// Phục vụ giao diện React Dashboard từ NuGet
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
                    Console.WriteLine("✅ ControlHub seeding completed successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ ControlHub seeding failed: {ex.Message}");
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