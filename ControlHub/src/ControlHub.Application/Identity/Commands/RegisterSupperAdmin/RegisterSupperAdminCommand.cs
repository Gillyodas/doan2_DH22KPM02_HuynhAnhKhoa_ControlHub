using ControlHub.Domain.Identity.Enums;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Identity.Commands.RegisterSupperAdmin
{
    public sealed record RegisterSupperAdminCommand(string Value, IdentifierType Type, string Password, string MasterKey, Guid? IdentifierConfigId = null) : IRequest<Result<Guid>>;
}
