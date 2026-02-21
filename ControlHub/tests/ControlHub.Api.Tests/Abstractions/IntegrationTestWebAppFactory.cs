using ControlHub.API;
using ControlHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ControlHub.Api.Tests.Abstractions;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = System.Guid.NewGuid().ToString();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                {"RoleSettings:SuperAdminRoleId", "9BA459E9-2A98-43C4-8530-392A63C66F1B"},
                {"RoleSettings:AdminRoleId", "0CD24FAC-ABD7-4AD9-A7E4-248058B8D404"},
                {"RoleSettings:UserRoleId", "8CF94B41-5AD8-4893-82B2-B193C91717AF"},
                {"TokenSettings:Secret", "SuperSecretKeyForTestingPurposesOnly123!MustBeLongEnough"},
                {"TokenSettings:Issuer", "ControlHub"},
                {"TokenSettings:Audience", "ControlHub"},
                {"TokenSettings:ExpiryMinutes", "60"},
                {"MasterKey", "MasterKey"}
            };
            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add InMemory Database with stable name for this factory
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });
        });
    }
}
