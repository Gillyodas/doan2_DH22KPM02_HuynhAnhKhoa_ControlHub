using ControlHub.Domain.Identity.Enums;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Identity.Commands.ForgotPassword
{
    public sealed record ForgotPasswordCommand(string Value, IdentifierType Type) : IRequest<Result>;
}
