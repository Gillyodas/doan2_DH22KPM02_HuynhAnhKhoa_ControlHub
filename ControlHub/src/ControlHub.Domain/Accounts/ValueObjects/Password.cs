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

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Convert.ToBase64String(Hash);
            yield return Convert.ToBase64String(Salt);
        }

        public bool IsValid()
        {
            // salt tối thiểu 16 bytes, hash tối thiểu 32 bytes (SHA-256 trở lên)
            return Hash is { Length: >= 32 }
                && Salt is { Length: >= 16 };
        }
    }
}
