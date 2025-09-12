using System.Security.Cryptography;
using System.Text;
using ControlHub.Application.Accounts.Interfaces.Security;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace ControlHub.Infrastructure.Accounts.Security;

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

    public Result<(byte[] Salt, byte[] Hash)> Hash(string password)
    {
        try
        {
            var salt = RandomNumberGenerator.GetBytes(_opt.SaltSize);
            var hash = Compute(password, salt,
                _opt.MemorySizeKB,
                _opt.Iterations,
                _opt.DegreeOfParallelism,
                _opt.HashSize);

            return Result<(byte[] Salt, byte[] Hash)>.Success((salt, hash));
        }
        catch (Exception ex)
        {
            return Result<(byte[] Salt, byte[] Hash)>.Failure("Password hashing failed", ex);
        }
    }

    public Result<bool> Verify(string password, byte[] salt, byte[] expected)
    {
        try
        {
            var actual = Compute(password, salt,
                _opt.MemorySizeKB,
                _opt.Iterations,
                _opt.DegreeOfParallelism,
                expected.Length);

            bool isMatch = CryptographicOperations.FixedTimeEquals(expected, actual);
            return isMatch
                ? Result<bool>.Success(true)
                : Result<bool>.Failure(AccountErrors.PasswordVerifyFailed.Code);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(AccountErrors.PasswordHashFailed.Code, ex);
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
