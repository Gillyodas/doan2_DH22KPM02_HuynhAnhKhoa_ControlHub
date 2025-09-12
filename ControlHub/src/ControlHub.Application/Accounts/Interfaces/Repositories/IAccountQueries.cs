using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Application.Accounts.Interfaces.Repositories
{
    public interface IAccountQueries
    {
        Task<Result<Maybe<Email>>> GetByEmail(Email email);
        Task<Result<Maybe<Account>>> GetAccountByEmail(Email email);
    }
}
