using ControlHub.Application.Accounts.DTOs;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Accounts.Interfaces.Security;
using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Accounts.Commands.SignIn
{
    public class SignInCommandHandler : IRequestHandler<SignInCommand, Result<SignInDTO>>
    {
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAccountQueries _accountQueries;
        private readonly ILogger<SignInCommandHandler> _logger;
        public SignInCommandHandler(IPasswordHasher passwordHasher, IAccountQueries accountQueries, ILogger<SignInCommandHandler> logger)
        {
            _passwordHasher = passwordHasher;
            _accountQueries = accountQueries;
            _logger = logger;
        }
        public async Task<Result<SignInDTO>> Handle(SignInCommand request, CancellationToken cancellationToken)
        {
            try
            {
                Result<Email> resultCreateEmail = Email.Create(request.email);

                if (!resultCreateEmail.IsSuccess)
                    return Result<SignInDTO>.Failure(AccountErrors.InvalidEmail.Code);

                var resultAccount = await _accountQueries.GetAccountByEmail(resultCreateEmail.Value, cancellationToken);

                var resultVerifyPassword = _passwordHasher.Verify(request.password, resultAccount.Password);

                if (!resultVerifyPassword)
                {
                    return Result<SignInDTO>.Failure(AccountErrors.InvalidCredentials.Code);
                }

                Account account = resultAccount;

                var dto = new SignInDTO(
                    account.Id,
                    account.User.Match(
                        some: u => u.Username,
                        none: () => "No name"
                    ),
                    "fake_jwt_here"
                );
                return Result<SignInDTO>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during SignIn for {Email}", request.email);

                return Result<SignInDTO>.Failure(
                    AccountErrors.UnexpectedError.Code,
                    ex
                );
            }
        }
    }
}
