using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Accounts.Interfaces.Security;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Domain.Common.Factories;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Accounts.Commands.CreateAccount
{
    public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Result<Guid>>
    {
        private readonly IAccountValidator _accountValidator;
        private readonly IAccountCommands _accountCommands;
        private readonly IPasswordHasher _passwordHasher;


        public CreateAccountCommandHandler(
            IAccountValidator accountValidator,
            IAccountCommands accountCommands,
            IPasswordHasher passwordHasher)
        {
            _accountValidator = accountValidator;
            _accountCommands = accountCommands;
            _passwordHasher = passwordHasher;
        }

        public async Task<Result<Guid>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            var emailResult = Email.Create(request.Email);
            if (!emailResult.IsSuccess)
                return Result<Guid>.Failure(emailResult.Error);

            var email = emailResult.Value;

            if (await _accountValidator.EmailIsExistAsync(email, cancellationToken))
            {
                return Result<Guid>.Failure(AccountErrors.EmailAlreadyExists.Code);
            }

            var accId = Guid.NewGuid();

            var passwordHashResult = _passwordHasher.Hash(request.Password);

            var accountResult = AccountFactory.CreateWithUser(accId, email, passwordHashResult);

            if (!accountResult.IsSuccess)
                return Result<Guid>.Failure(accountResult.Error);

            await _accountCommands.AddAsync(accountResult.Value.Value, cancellationToken);

            return Result<Guid>.Success(accId);
        }
    }
}
