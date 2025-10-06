using ControlHub.API.Accounts.ViewModels.Request;
using ControlHub.Application.Accounts.Commands.ForgotPassword;
using ControlHub.Domain.Accounts.Enums;

namespace ControlHub.API.Accounts.Mappers
{
    public static class ForgotPasswordRequestMapper
    {
        public static ForgotPasswordCommand ToCommand(ForgotPasswordRequest request)
        {
            if (!Enum.TryParse<IdentifierType>(request.Type, true, out var type))
                throw new ArgumentException("Unsupported identifier type");

            return new ForgotPasswordCommand(request.Value, type);
        }
    }
}