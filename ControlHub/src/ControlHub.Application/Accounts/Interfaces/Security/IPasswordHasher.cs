namespace ControlHub.Application.Accounts.Interfaces.Security
{
    public interface IPasswordHasher
    {
        (byte[] Salt, byte[] Hash) Hash(string password);
        bool Verify(string password, string hash);
    }
}
