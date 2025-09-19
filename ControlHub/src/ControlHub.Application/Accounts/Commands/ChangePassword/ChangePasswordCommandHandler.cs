using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Accounts.Interfaces.Security;
using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Accounts.Commands.ChangePassword
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
    {
        private readonly IAccountQueries _accountQueries;
        private readonly IAccountCommands _accountCommands;
        private readonly IPasswordHasher _passwordHasher;

        public ChangePasswordCommandHandler(IAccountQueries accountQueries, IAccountCommands accountCommands, IPasswordHasher passwordHasher)
        {
            _accountQueries = accountQueries;
            _accountCommands = accountCommands;
            _passwordHasher = passwordHasher;
        }

        public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            Account acc = await _accountQueries.GetAccountWithoutUserById(request.id, cancellationToken);
            if (acc is null)
                return Result.Failure(AccountErrors.AccountNotFound.Code);

            var passIsVerify = _passwordHasher.Verify(request.curPassword, acc.Password);
            if (!passIsVerify)
                return Result.Failure(AccountErrors.InvalidCredentials.Code);

            Password newPass = _passwordHasher.Hash(request.newPassword);

            var updateResult = acc.UpdatePassword(newPass);
            if (!updateResult.IsSuccess)
                return updateResult;

            await _accountCommands.UpdateAsync(acc, cancellationToken);

            return Result.Success();
        }
    }
}
