using System.Security.Cryptography;
using System.Text;
using ControlHub.Application.Accounts.Interfaces.Security;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace ControlHub.Infrastructure.Security;

public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private readonly Argon2Options _opt;
    public Argon2PasswordHasher(IOptions<Argon2Options> opt)
    {
        _opt = opt.Value ?? throw new ArgumentNullException(nameof(opt));

        if (_opt.SaltSize <= 0)
            throw new ArgumentException("SaltSize must be > 0", nameof(_opt.SaltSize));
        if (_opt.HashSize <= 0)
            throw new ArgumentException("HashSize must be > 0", nameof(_opt.HashSize));
        if (_opt.MemorySizeKB <= 0)
            throw new ArgumentException("MemorySizeKB must be > 0", nameof(_opt.MemorySizeKB));
        if (_opt.Iterations <= 0)
            throw new ArgumentException("Iterations must be > 0", nameof(_opt.Iterations));
        if (_opt.DegreeOfParallelism <= 0)
            throw new ArgumentException("DegreeOfParallelism must be > 0", nameof(_opt.DegreeOfParallelism));
    }

    public (byte[] Salt, byte[] Hash) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(_opt.SaltSize);
        var bytes = Compute(password, salt, _opt.MemorySizeKB, _opt.Iterations, _opt.DegreeOfParallelism, _opt.HashSize);

        return (salt, bytes);
    }

    public bool Verify(string password, string phc)
    {
        try
        {
            var parts = phc.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5 || parts[0] != "argon2id") return false;

            var pars = parts[2].Split(',');
            if (pars.Length < 3) return false;

            int m = int.Parse(pars[0].Split('=')[1]);
            int t = int.Parse(pars[1].Split('=')[1]);
            int p = int.Parse(pars[2].Split('=')[1]);

            var salt = Convert.FromBase64String(parts[3]);
            var expected = Convert.FromBase64String(parts[4]);

            var actual = Compute(password, salt, m, t, p, expected.Length);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch
        {
            // bất kỳ lỗi nào (format sai, base64 sai, etc) → return false thay vì throw
            return false;
        }
    }

    private static byte[] Compute(string pwd, byte[] salt, int m, int t, int p, int len)
    {
        var argon = new Argon2id(Encoding.UTF8.GetBytes(pwd))
        {
            Salt = salt,
            MemorySize = m,
            Iterations = t,
            DegreeOfParallelism = p
        };
        return argon.GetBytes(len);
    }
}
