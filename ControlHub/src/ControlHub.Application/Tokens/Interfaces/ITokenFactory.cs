using ControlHub.Domain.Tokens;
using ControlHub.Domain.Tokens.Enums;

namespace ControlHub.Application.Tokens.Interfaces
{
    public interface ITokenFactory
    {
        public Token Create(Guid accountId, string value, TokenType type);
    }
}
