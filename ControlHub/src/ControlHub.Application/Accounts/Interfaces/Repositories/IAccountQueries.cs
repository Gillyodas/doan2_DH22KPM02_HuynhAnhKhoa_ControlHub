using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Application.Accounts.Interfaces.Repositories
{
    public interface IAccountQueries
    {
        Task<Result<Maybe<Email>>> GetByEmail(Email email);
    }
}
