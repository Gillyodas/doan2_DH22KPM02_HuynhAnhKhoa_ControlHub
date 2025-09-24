using System;
using System.Collections.Generic;
using System.Linq;
using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Emails.Interfaces;
using ControlHub.Application.Tokens.Interfaces;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Accounts.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
    {
        private readonly IPasswordResetTokenGenerator _passwordResetTokenGenerator;
        private readonly IAccountQueries _accountQueries;
        private readonly IAccountValidator _accountValidator;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ForgotPasswordCommandHandler> _logger;

        public ForgotPasswordCommandHandler(
            IPasswordResetTokenGenerator passwordResetTokenGenerator,
            IAccountQueries accountQueries,
            IAccountValidator accountValidator,
            IEmailSender emailSender,
            ILogger<ForgotPasswordCommandHandler> logger)
        {
            _passwordResetTokenGenerator = passwordResetTokenGenerator;
            _accountQueries = accountQueries;
            _accountValidator = accountValidator;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Code}: {Message} for Email {Email}",
                AccountLogs.ForgotPassword_Started.Code,
                AccountLogs.ForgotPassword_Started.Message,
                request.email);

            var emailResult = Email.Create(request.email);
            if (!emailResult.IsSuccess)
            {
                _logger.LogWarning("{Code}: {Message} for Email {Email}",
                    AccountLogs.ForgotPassword_InvalidEmail.Code,
                    AccountLogs.ForgotPassword_InvalidEmail.Message,
                    request.email);

                return Result<string>.Failure(emailResult.Error);
            }

            var acc = await _accountQueries.GetAccountByEmail(emailResult.Value, cancellationToken);

            if (acc is null)
            {
                _logger.LogWarning("{Code}: {Message} for Email {Email}",
                    AccountLogs.ForgotPassword_EmailNotFound.Code,
                    AccountLogs.ForgotPassword_EmailNotFound.Message,
                    request.email);

                return Result<string>.Failure(AccountErrors.EmailNotFound.Code);
            }

            string resetToken = _passwordResetTokenGenerator.Generate(acc.Id.ToString());

            _logger.LogInformation("{Code}: {Message} for AccountId {AccountId}",
                AccountLogs.ForgotPassword_TokenGenerated.Code,
                AccountLogs.ForgotPassword_TokenGenerated.Message,
                acc.Id);

            var resetLink = $"https://your-app/reset-password?token={resetToken}";
            var subject = "Reset your password";
            var body = $"Click this link to reset your password: <a href='{resetLink}'>Reset Password</a>";

            await _emailSender.SendEmailAsync(request.email, subject, body);

            _logger.LogInformation("{Code}: {Message} for Email {Email}",
                AccountLogs.ForgotPassword_EmailSent.Code,
                AccountLogs.ForgotPassword_EmailSent.Message,
                request.email);

            return Result.Success();
        }
    }
}
