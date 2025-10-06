using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlHub.Application.Tokens.Interfaces
{
    public interface IAccessTokenGenerator
    {
        string Generate(string accId, string identifier, IEnumerable<string> roles);
    }
}
