using System.Text;
using ControlHub.API.Configurations;
using ControlHub.API.Middlewares;
using ControlHub.Application.Common.Behaviors;
using ControlHub.Infrastructure.Tokens;
using ControlHub.Infrastructure.Tokens.Generate;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;

namespace ControlHub.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = TokenVerifier.GetValidationParameters();
            });

            // --- Register AutoMapper ---
            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(typeof(ControlHub.Application.AssemblyReference).Assembly);
                cfg.AddMaps(typeof(ControlHub.Infrastructure.AssemblyReference).Assembly);
            });

            // Add Authentication + JWT config
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                    };
                });

            builder.Services.AddAuthorization();

            // Config Serilog
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "ControlHub.API")
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("Logs/log-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
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
                        options.Endpoint = new Uri("http://otel-collector:4317"); // dùng HTTP OTLP
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


            // Config MediatR
            builder.Services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(ControlHub.Application.AssemblyReference.Assembly));
            builder.Services.AddValidatorsFromAssembly(ControlHub.Application.AssemblyReference.Assembly);
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            // Load extra config files BEFORE services use them
            builder.Configuration
                .AddJsonFile("Configurations/DBSettings.json", optional: true, reloadOnChange: true);

            // Add services to the container.

            // 1. API level ***************************************************************************************

            builder.Services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen();

            //*****************************************************************************************************



            // 2. Infrastructure **********************************************************************************

            // Register Infrastructure service identifiers
            builder.Services.AddInfrastructure();

            // Register DbContext
            builder.Services.AddDatabase(builder.Configuration);

            // Register TokenSettings
            builder.Services.Configure<TokenSettings>(
                builder.Configuration.GetSection("TokenSettings"));

            //*****************************************************************************************************


            var app = builder.Build();

            // Middlewares
            app.UseMiddleware<GlobalExceptionMiddleware>();

            // Metrics endpoint cho Prometheus
            app.MapMetrics();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();

                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseGlobalExceptionMiddleware();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
