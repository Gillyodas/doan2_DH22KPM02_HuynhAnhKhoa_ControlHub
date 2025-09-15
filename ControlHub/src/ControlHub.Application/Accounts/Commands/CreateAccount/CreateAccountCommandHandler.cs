using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Accounts.Interfaces.Security;
using ControlHub.Domain.Accounts;
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

        private async Task<Result<Unit>> ValidateRequestAsync(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            // Validate Email (Domain VO check)
            var emailResult = Email.Create(request.Email);
            if (!emailResult.IsSuccess)
                return Result<Unit>.Failure(emailResult.Error);

            var email = emailResult.Value;

            // Check Email Exists (Application check qua Repository/Validator)
            var emailIsExist = await _accountValidator.EmailIsExistAsync(email);
            if (!emailIsExist.IsSuccess)
                return Result<Unit>.Failure(emailIsExist.Error);

            if (emailIsExist.Value)
                return Result<Unit>.Failure("Email already exists");

            return Result<Unit>.Success(Unit.Value);
        }

        public async Task<Result<Guid>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            var emailResult = Email.Create(request.Email);
            if (!emailResult.IsSuccess)
                return Result<Guid>.Failure(emailResult.Error);

            var email = emailResult.Value;

            var emailIsExist = await _accountValidator.EmailIsExistAsync(email);

            if (!emailIsExist.IsSuccess)
            {
<<<<<<< Updated upstream
                var emailResult = Email.Create(request.Email);
                if (!emailResult.IsSuccess)
                    return Result<Guid>.Failure(emailResult.Error);

                var email = emailResult.Value;

                var emailIsExist = await _accountValidator.EmailIsExistAsync(email);

                if (!emailIsExist.IsSuccess)
                {
                    return Result<Guid>.Failure(emailIsExist.Error);
                }

                if (emailIsExist.Value)
                    return Result<Guid>.Failure("Email already exists");

                var accId = Guid.NewGuid();

                var (salt, hash) = _passwordHasher.Hash(request.Password);

                var accountResult = AccountFactory.CreateWithUser(accId, email, hash, salt);

                if (!accountResult.IsSuccess)
                    return Result<Guid>.Failure(accountResult.Error);

                // LƯU account
                var insertResult = await _accountCommands.AddAsync(accountResult.Value.Value);

                if (!insertResult.IsSuccess)
                    return Result<Guid>.Failure(insertResult.Error);

                return Result<Guid>.Success(accId);
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure("UC error", ex);
=======
                return Result<Guid>.Failure(emailIsExist.Error);
>>>>>>> Stashed changes
            }

            if (emailIsExist.Value)
                return Result<Guid>.Failure("Email already exists");

            var accId = Guid.NewGuid();

            var passwordHashResult = _passwordHasher.Hash(request.Password);

            if(!passwordHashResult.IsSuccess)
                return Result<Guid>.Failure(emailIsExist.Error);

            var accountResult = AccountFactory.CreateWithUser(accId, email, passwordHashResult.Value.Hash, passwordHashResult.Value.Salt);

            if (!accountResult.IsSuccess)
                return Result<Guid>.Failure(accountResult.Error);

            await _accountCommands.AddAsync(accountResult.Value.Value);

            return Result<Guid>.Success(accId);
        }
    }
}
