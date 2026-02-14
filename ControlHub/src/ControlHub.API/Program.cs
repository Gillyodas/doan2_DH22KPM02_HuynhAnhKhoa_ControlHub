using ControlHub.Infrastructure.RealTime.Hubs;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;

namespace ControlHub.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Load AI system knowledge config
            builder.Configuration.AddJsonFile("AI/system_knowledge.json", optional: true, reloadOnChange: true);

            // =========================================================================
            // 1. HOST CONFIGURATION (Logging, Metrics, Tracing)
            // Phần này thuộc về "Ứng dụng chứa" (Host App). 
            // Người dùng thư viện có thể muốn dùng NLog thay vì Serilog, hoặc Jaeger thay vì Prometheus.
            // Nên để họ tự quyết định ở đây.
            // =========================================================================

            // Config Serilog
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "ControlHub.API")
                // Reduce noise from EF Core (hide SQL queries in console/log)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
                // Sub-logger: Write only Email logs to a separate file
                .WriteTo.Logger(l => l
                    .Filter.ByIncludingOnly(e => e.Properties.TryGetValue("SourceContext", out var v) && v.ToString().Contains("ControlHub.Infrastructure.Emails"))
                    .WriteTo.File(new RenderedCompactJsonFormatter(), "Logs/email-.json", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14, shared: true))
                // Main logger: Write everything ELSE to console and main file
                .WriteTo.Logger(l => l
                    .Filter.ByExcluding(e => e.Properties.TryGetValue("SourceContext", out var v) && v.ToString().Contains("ControlHub.Infrastructure.Emails"))
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(new RenderedCompactJsonFormatter(), "Logs/log-.json", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14, shared: true))
                .CreateLogger();

            builder.Host.UseSerilog();

            // Config OpenTelemetry
            builder.Services.AddOpenTelemetry()
                .WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri("http://otel-collector:4317");
                            options.Protocol = OtlpExportProtocol.Grpc;
                        });
                })
                .WithMetrics(meterProviderBuilder =>
                {
                    meterProviderBuilder
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri("http://otel-collector:4318");
                        });
                });

            // =========================================================================
            // 2. CONTROL HUB LIBRARY (CORE LOGIC)
            // Đây là dòng quan trọng nhất. Toàn bộ logic nghiệp vụ, DB, Auth nằm ở đây.
            // =========================================================================

            builder.Services.AddControlHub(builder.Configuration);

            builder.Services.AddMemoryCache();

            // =========================================================================
            // 3. BUILD & PIPELINE
            // =========================================================================

            var app = builder.Build();

            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.MapMetrics(); // Prometheus Endpoint

            // CORS Configuration
            app.UseCors(policy => policy
                .WithOrigins("http://localhost:3000", "http://localhost:3000/control-hub", "https://localhost:7110")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());

            if (app.Environment.IsDevelopment())
            {
                app.UseStaticFiles(); // Serve static files like CSS
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.InjectStylesheet("/custom-swagger.css");
                    options.RoutePrefix = "swagger";
                });
            }

            app.UseHttpsRedirection();

            // Authentication & Authorization Middleware phải được gọi ở Host App
            // để đảm bảo đúng thứ tự trong Pipeline của họ.
            app.UseAuthentication();
            app.UseAuthorization();

            // Kích hoạt ControlHub (Auto Migration & Seed Data)
            app.UseControlHub();

            app.MapHub<DashboardHub>("/hubs/dashboard");

            app.MapControllers();

            app.Run();
        }
    }
}