using ControlHub.SharedKernel.Results;

namespace ControlHub.Application.Accounts.Interfaces.Security
{
    public interface IPasswordHasher
    {
        Result<(byte[] Salt, byte[] Hash)> Hash(string password);
        Result<bool> Verify(string password, byte[] salt, byte[] expected);
    }
}
