using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Accounts.Interfaces.Security;
using ControlHub.Application.Tokens.Interfaces;
using ControlHub.Application.Users.Interfaces.Repositories;
using ControlHub.Application.Emails.Interfaces;
using ControlHub.Infrastructure.Accounts.Repositories;
using ControlHub.Infrastructure.Accounts.Security;
using ControlHub.Infrastructure.Accounts.Validators;
using ControlHub.Infrastructure.Emails;
using ControlHub.Infrastructure.Tokens.Generate;
using ControlHub.Infrastructure.Users.Repositories;

namespace ControlHub.API.Configurations
{
    public static class DI
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            //Securities
            services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();

            //Token.Generator
            services.AddScoped<IAccessTokenGenerator, AccessTokenGenerator>();
            services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
            services.AddScoped<IPasswordResetTokenGenerator, PasswordResetTokenGenerator>();
            services.AddScoped<IEmailConfirmationTokenGenerator, EmailConfirmationTokenGenerator>();

            //Email
            services.AddScoped<IEmailSender, SmtpEmailSender>();

            //Account.Validator
            services.AddScoped<IAccountValidator, AccountValidator>();

            //Account.Repositories
            services.AddScoped<IAccountQueries, AccountQueries>();
            services.AddScoped<IAccountCommands, AccountCommands>();

            //User.Repositories
            services.AddScoped<IUserCommands, UserCommands>();
            services.AddScoped<IUserQueries, UserQueries>();


            return services;
        }
    }
}
