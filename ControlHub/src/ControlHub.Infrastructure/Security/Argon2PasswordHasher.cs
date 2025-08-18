using System.Security.Cryptography;
using System.Text;
using ControlHub.Application.Interfaces.Security;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace ControlHub.Infrastructure.Security;

public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private readonly Argon2Options _opt;
    public Argon2PasswordHasher(IOptions<Argon2Options> opt) => _opt = opt.Value;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(_opt.SaltSize);
        var bytes = Compute(password, salt, _opt.MemorySizeKB, _opt.Iterations, _opt.DegreeOfParallelism, _opt.HashSize);

        // PHC string: $argon2id$v=19$m=...,t=...,p=...$base64(salt)$base64(hash)
        return $"$argon2id$v=19$m={_opt.MemorySizeKB},t={_opt.Iterations},p={_opt.DegreeOfParallelism}$" +
               $"{Convert.ToBase64String(salt)}$" +
               $"{Convert.ToBase64String(bytes)}";
    }

    public bool Verify(string password, string phc)
    {
        var parts = phc.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5 || parts[0] != "argon2id") return false;

        var pars = parts[2].Split(',');
        int m = int.Parse(pars[0].Split('=')[1]);
        int t = int.Parse(pars[1].Split('=')[1]);
        int p = int.Parse(pars[2].Split('=')[1]);

        var salt = Convert.FromBase64String(parts[3]);
        var expected = Convert.FromBase64String(parts[4]);

        var actual = Compute(password, salt, m, t, p, expected.Length);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
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
