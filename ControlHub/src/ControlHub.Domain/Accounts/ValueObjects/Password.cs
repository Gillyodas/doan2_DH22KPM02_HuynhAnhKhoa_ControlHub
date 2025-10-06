using ControlHub.Domain.Accounts.Interfaces.Security;
using ControlHub.Domain.Common;

namespace ControlHub.Domain.Accounts.ValueObjects
{
    public sealed class Password : ValueObject
    {
        public byte[] Hash { get; private set; }
        public byte[] Salt { get; private set; }

        private Password(byte[] hash, byte[] salt)
        {
            Hash = hash;
            Salt = salt;
        }

        public static Password From(byte[] hash, byte[] salt) => new(hash, salt);

        public static Password? Create(string passStr, IPasswordHasher passwordHasher)
        {
            var pass = passwordHasher.Hash(passStr);

            if (!pass.IsValid())
                return null;

            return pass;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Convert.ToBase64String(Hash);
            yield return Convert.ToBase64String(Salt);
        }

        public bool IsValid()
        {
            // Salt: tối thiểu 16 bytes, tối đa 64 bytes
            // Hash: tối thiểu 32 bytes (SHA-256), tối đa 64 bytes (SHA-512)
            return Hash is { Length: >= 32 and <= 64 }
                && Salt is { Length: >= 16 and <= 64 };
        }
    }
}
