using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ControlHub.Infrastructure.Persistence
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // Dùng connection string tạm cho migration
            // Khi chạy runtime, API sẽ override bằng appsettings.json
            optionsBuilder.UseSqlServer("Server=localhost;Database=ControlHub;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}