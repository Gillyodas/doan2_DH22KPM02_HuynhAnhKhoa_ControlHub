using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Application.Accounts.Interfaces.Security
{
    public interface IPasswordHasher
    {
        Password Hash(string password);
        bool Verify(string password, Password accPass);
    }
}
