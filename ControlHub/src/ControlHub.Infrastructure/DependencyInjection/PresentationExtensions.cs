using System.Reflection;
using ControlHub.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

internal static class PresentationExtensions
{
    internal static IServiceCollection AddControlHubPresentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var appAssembly = typeof(ControlHub.Application.AssemblyReference).Assembly;
        var infraAssembly = typeof(ControlHub.Infrastructure.AssemblyReference).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(appAssembly);
            cfg.RegisterServicesFromAssembly(infraAssembly);
        });
        services.AddValidatorsFromAssembly(appAssembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(appAssembly);
            cfg.AddMaps(infraAssembly);
        });

        services.AddControllers()
            .AddApplicationPart(Assembly.GetExecutingAssembly());

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            var dashboardUrl = configuration["ControlHub:DashboardUrl"] ?? "/control-hub/index.html";

            c.CustomSchemaIds(type =>
        type.FullName?
            .Replace("`1[[", "_")
            .Replace("]]", "")
            .Replace(", ", "_")
            .Replace(",", "_")
            ?? type.Name);

            c.AddServer(new Microsoft.OpenApi.Models.OpenApiServer
            {
                Url = "https://localhost:7110"
            });

            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ControlHub Identity API",
                Version = "v1",
                Description = $@"Identity & Access Management provided by ControlHub NuGet

<a href='{dashboardUrl}' class='my-custom-button'>🚀 Open ControlHub Dashboard</a>

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

            c.CustomSchemaIds(type => type.FullName);
        });

        return services;
    }
}