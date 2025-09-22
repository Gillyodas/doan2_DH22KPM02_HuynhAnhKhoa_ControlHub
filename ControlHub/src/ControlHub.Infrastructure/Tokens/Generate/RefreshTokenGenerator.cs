using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ControlHub.Infrastructure.Tokens.Generate
{
    public class RefreshTokenGenerator
    {
        public string Generate()
        {
            // Refresh token không cần JWT, thường là random string
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }
    }
}
