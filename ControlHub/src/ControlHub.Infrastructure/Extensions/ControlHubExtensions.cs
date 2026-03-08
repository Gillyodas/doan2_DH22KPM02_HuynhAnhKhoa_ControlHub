using System.Reflection;
using ControlHub.Infrastructure.DependencyInjection;
using ControlHub.Infrastructure.Persistence;
using ControlHub.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace ControlHub;

public static class ControlHubExtensions
{
    public static IServiceCollection AddControlHub(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddControlHubDatabase(configuration)
            .AddControlHubSecurity(configuration)
            .AddControlHubTokenManagement(configuration)
            .AddControlHubIdentity()
            .AddControlHubAccessControl(configuration)
            .AddControlHubMessaging()
            .AddControlHubAi(configuration)
            .AddControlHubObservability()
            .AddControlHubPresentation(configuration);

        return services;
    }

    public static IApplicationBuilder UseControlHub(
    this IApplicationBuilder app,
    string guiPath = "/control-hub")
    {
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
                db.Database.Migrate();

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
        const string resourcePrefix = "ControlHub.Infrastructure.Resources.GUI";

        app.Map(guiPath, builder =>
        {
            builder.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value?.TrimStart('/');
                bool isApiCall = path?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true;
                bool isStaticFile = path?.Contains('.') == true
                    && !path.EndsWith(".html", StringComparison.OrdinalIgnoreCase);

                if (!isApiCall && !isStaticFile)
                {
                    await ServeIndexHtml(assembly, context);
                    return;
                }

                await next();
            });

            builder.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new EmbeddedFileProvider(assembly, resourcePrefix),
                RequestPath = ""
            });

            builder.Run(async context => await ServeIndexHtml(assembly, context));
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
}