using Microsoft.EntityFrameworkCore;
using ControlHub.Infrastructure.Persistence;

namespace ControlHub.API.Configurations
{
    public static class DBConfig
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                ));

            return services;
        }
    }
}
