using System.Security.Cryptography;
using ControlHub.Application.Tokens.Interfaces.Generate;

namespace ControlHub.Infrastructure.TokenManagement.Services.Generate
{
    internal class RefreshTokenGenerator : IRefreshTokenGenerator
    {
        public string Generate()
        {
            // Refresh token kh�ng c?n JWT, thu?ng l� random string
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }
    }
}
