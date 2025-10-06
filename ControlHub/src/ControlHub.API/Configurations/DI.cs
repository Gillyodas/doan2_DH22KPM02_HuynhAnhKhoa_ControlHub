using ControlHub.Application.Accounts.Identifiers.Interfaces;
using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Emails.Interfaces;
using ControlHub.Application.OutBoxs;
using ControlHub.Application.OutBoxs.Repositories;
using ControlHub.Application.Tokens.Interfaces;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Application.Tokens.Interfaces.Sender;
using ControlHub.Application.Users.Interfaces.Repositories;
using ControlHub.Domain.Accounts.Identifiers.Interfaces;
using ControlHub.Domain.Accounts.Interfaces.Security;
using ControlHub.Infrastructure.Accounts.Repositories;
using ControlHub.Infrastructure.Accounts.Security;
using ControlHub.Infrastructure.Accounts.Validators;
using ControlHub.Infrastructure.Emails;
using ControlHub.Infrastructure.Identifiers.Validator;
using ControlHub.Infrastructure.Outboxs;
using ControlHub.Infrastructure.Outboxs.Handler;
using ControlHub.Infrastructure.Outboxs.Repositories;
using ControlHub.Infrastructure.Persistence;
using ControlHub.Infrastructure.Tokens;
using ControlHub.Infrastructure.Tokens.Generate;
using ControlHub.Infrastructure.Tokens.Repositories;
using ControlHub.Infrastructure.Tokens.Sender;
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

            //Token.Sender
            services.AddScoped<ITokenSender, EmailTokenSender>();
            services.AddScoped<ITokenSender, SmsTokenSender>();
            services.AddScoped<ITokenSenderFactory, TokenSenderFactory>();

            //Token.Factory
            services.AddScoped<ITokenFactory, TokenFactory>();

            //Token.Repositories
            services.AddScoped<ITokenQueries, TokenQueries>();
            services.AddScoped<ITokenCommands, TokenCommands>();

            //Email
            services.AddScoped<IEmailSender, SmtpEmailSender>();

            //Account.Validator
            services.AddScoped<IAccountValidator, AccountValidator>();

            //Account.Factory
            services.AddScoped<IIdentifierValidatorFactory, IdentifierValidatorFactory>();

            //Account.Repositories
            services.AddScoped<IAccountQueries, AccountQueries>();
            services.AddScoped<IAccountCommands, AccountCommands>();

            //Identifier.Validator
            services.AddScoped<IIdentifierValidator, EmailIdentifierValidator>();
            services.AddScoped<IIdentifierValidator, UsernameIdentifierValidator>();
            services.AddScoped<IIdentifierValidator, PhoneIdentifierValidator>();

            //User.Repositories
            services.AddScoped<IUserCommands, UserCommands>();
            services.AddScoped<IUserQueries, UserQueries>();

            //Persistence
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            //Outbox.Repositories
            services.AddScoped<IOutboxCommands, OutboxCommands>();

            //Outbox.Handlers
            services.AddScoped<IOutboxHandler, EmailOutboxHandler>();

            //Outboxs.Factory
            services.AddScoped<OutboxHandlerFactory>();

            //Processor background service
            services.AddHostedService<OutboxProcessor>();

            return services;
        }
    }
}
