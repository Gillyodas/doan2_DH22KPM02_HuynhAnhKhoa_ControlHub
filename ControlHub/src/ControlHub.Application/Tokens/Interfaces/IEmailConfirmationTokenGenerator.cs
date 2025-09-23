using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlHub.Application.Tokens.Interfaces
{
    public interface IEmailConfirmationTokenGenerator
    {
        string Generate(string userId, string email);
    }
}
