using ControlHub.Domain.TokenManagement.Aggregates;
using ControlHub.Domain.TokenManagement.Enums;

namespace ControlHub.Application.TokenManagement.Interfaces
{
    public interface ITokenFactory
    {
        public Token Create(Guid accountId, string value, TokenType type);
    }
}
