using ControlHub.Infrastructure.Security;
using ControlHub.Application.Accounts.Interfaces.Security;
using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Infrastructure.Accounts.Validators;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Infrastructure.Accounts.Repositories;
using ControlHub.Application.Users.Interfaces.Repositories;
using ControlHub.Infrastructure.Users.Repositories;

namespace ControlHub.API.Configurations
{
    public static class DI
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            //Securities
            services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();

            //Account.Validator
            services.AddScoped<IAccountValidator, AccountValidator>();

            //Account.Repositories
            services.AddScoped<IAccountQueries, AccountQueries>();
            services.AddScoped<IAccountCommands, AccountCommands>();

            //User.Repositories
            services.AddScoped<IUserCommands, UserCommands>();


            return services;
        }
    }
}
