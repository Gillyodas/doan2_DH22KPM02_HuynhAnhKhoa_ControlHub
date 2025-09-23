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

namespace ControlHub.Application.Accounts.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
    {
        private readonly IPasswordResetTokenGenerator _passwordResetTokenGenerator;
        private readonly IAccountQueries _accountQueries;
        private readonly IAccountValidator _accountValidator;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordCommandHandler(IPasswordResetTokenGenerator passwordResetTokenGenerator, IAccountQueries accountQueries, IAccountValidator accountValidator, IEmailSender emailSender)
        {
            _passwordResetTokenGenerator = passwordResetTokenGenerator;
            _accountQueries = accountQueries;
            _accountValidator = accountValidator;
            _emailSender = emailSender;
        }
        public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var emailResult = Email.Create(request.email);
            if (!emailResult.IsSuccess)
                return Result<string>.Failure(emailResult.Error);

            var acc = await _accountQueries.GetAccountByEmail(emailResult.Value, cancellationToken);

            if (acc is null)
                return Result<string>.Failure(AccountErrors.EmailNotFound.Code);

            string resetToken = _passwordResetTokenGenerator.Generate(acc.Id.ToString());

            var resetLink = $"https://your-app/reset-password?token={resetToken}";
            var subject = "Reset your password";
            var body = $"Click this link to reset your password: <a href='{resetLink}'>Reset Password</a>";

            await _emailSender.SendEmailAsync(request.email, subject, body);

            return Result.Success();
        }
    }
}
