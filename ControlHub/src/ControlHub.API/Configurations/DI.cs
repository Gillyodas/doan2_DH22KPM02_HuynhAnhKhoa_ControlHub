using ControlHub.Infrastructure.Security;
using ControlHub.Application.Interfaces.Security;

namespace ControlHub.API.Configurations
{
    public static class DI
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
            return services;
        }
    }
}
