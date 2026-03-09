using ControlHub.Domain.Identity.Enums;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Identity.Commands.RegisterAdmin
{
    public sealed record RegisterAdminCommand(string Value, IdentifierType Type, string Password, Guid? IdentifierConfigId = null) : IRequest<Result<Guid>>;
}
