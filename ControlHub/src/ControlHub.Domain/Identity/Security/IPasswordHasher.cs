using ControlHub.Domain.Identity.ValueObjects;

namespace ControlHub.Domain.Identity.Security
{
    public interface IPasswordHasher
    {
        Password Hash(string password);
        bool Verify(string password, Password accPass);
    }
}
