using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ControlHub.Infrastructure.Persistence
{
    internal class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        // Design-time factory phải có constructor rỗng mặc định
        public AppDbContext CreateDbContext(string[] args)
        {
            // 1. Tự build Configuration thủ công (vì không có DI lúc design-time)
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.Development.json", optional: true)
                .Build();

            var builder = new DbContextOptionsBuilder<AppDbContext>();

            // 2. Lấy connection string
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // 3. Cấu hình DbContext
            builder.UseSqlServer(connectionString, b =>
                b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

            return new AppDbContext(builder.Options);
        }
    }
}