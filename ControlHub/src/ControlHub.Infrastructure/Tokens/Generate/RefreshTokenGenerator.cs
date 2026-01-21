using System.Security.Cryptography;
using ControlHub.Application.Tokens.Interfaces.Generate;

namespace ControlHub.Infrastructure.Tokens.Generate
{
    internal class RefreshTokenGenerator : IRefreshTokenGenerator
    {
        public string Generate()
        {
            // Refresh token không cần JWT, thường là random string
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }
    }
}
