using ControlHub.API.Roles.ViewModels.Requests;
using ControlHub.API.Roles.ViewModels.Responses;
using ControlHub.Application.Roles.Commands.CreateRoles;
using ControlHub.Domain.Roles;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ControlHub.API.Roles
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly IMediator _mediator;
        public RoleController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPost("roles")]
        public async Task<IActionResult> CreateRoles([FromBody] CreateRolesRequest request, CancellationToken ct)
        {
            var command = new CreateRolesCommand(request.Roles);
            var result = await _mediator.Send(command, ct);

            if (result.IsFailure)
            {

                return BadRequest(new CreateRolesResponse
                {
                    Message = result.Error.Message,
                    SuccessCount = 0,
                    FailureCount = request.Roles.Count()
                });
            }

            if (result is Result<PartialResult<Role, string>> typedResult)
            {
                var summary = typedResult.Value;
                return Ok(new CreateRolesResponse
                {
                    Message = summary.Failures.Any()
                        ? "Partial success: some roles failed to create."
                        : "All roles created successfully.",
                    SuccessCount = summary.Successes.Count(),
                    FailureCount = summary.Failures.Count(),
                    FailedRoles = summary.Failures
                });
            }

            // fallback – nếu handler chỉ trả về Result.Success()
            return Ok(new CreateRolesResponse
            {
                Message = "All roles created successfully.",
                SuccessCount = request.Roles.Count(),
                FailureCount = 0
            });
        }
    }
}
