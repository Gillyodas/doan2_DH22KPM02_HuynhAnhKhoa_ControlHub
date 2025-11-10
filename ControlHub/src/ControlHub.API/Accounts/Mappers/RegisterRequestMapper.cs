using ControlHub.API.Accounts.ViewModels.Request;
using ControlHub.Application.Accounts.Commands.CreateAccount;
using ControlHub.Domain.Accounts.Enums;

namespace ControlHub.API.Accounts.Mappers
{
    public static class RegisterRequestMapper
    {
        public static CreateAccountCommand ToCommand(RegisterRequest request)
        {
            if (!Enum.TryParse<IdentifierType>(request.Type, ignoreCase: true, out var type))
                throw new ArgumentException("Unsupported identifier type");

            return new CreateAccountCommand(request.Value, type, request.Password);
        }
    }
}
