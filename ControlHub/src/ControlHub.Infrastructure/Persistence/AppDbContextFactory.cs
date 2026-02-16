using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ControlHub.Infrastructure.Persistence
{
    internal class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        // Design-time factory ph?i có constructor r?ng m?c d?nh
        public AppDbContext CreateDbContext(string[] args)
        {
            // 1. T? build Configuration th? công (vì không có DI lúc design-time)
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.Development.json", optional: true)
                .Build();

            var builder = new DbContextOptionsBuilder<AppDbContext>();

            // 2. L?y connection string
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // 3. C?u hình DbContext
            builder.UseSqlServer(connectionString, b =>
                b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

            return new AppDbContext(builder.Options);
        }
    }
}
