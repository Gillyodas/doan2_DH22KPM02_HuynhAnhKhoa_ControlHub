using ControlHub.API.Accounts.ViewModels.Request;
using ControlHub.Application.Accounts.Commands.CreateAccount;
using ControlHub.Application.Accounts.Commands.SignIn;
using ControlHub.Domain.Accounts.Enums;

namespace ControlHub.API.Accounts.Mappers
{
    public static class SignInRequestMapper
    {
        public static SignInCommand ToCommand(SignInRequest request)
        {
            if (!Enum.TryParse<IdentifierType>(request.Type, ignoreCase: true, out var type))
                throw new ArgumentException("Unsupported identifier type");

            return new SignInCommand(request.Value, request.Password, type);
        }
    }
}
