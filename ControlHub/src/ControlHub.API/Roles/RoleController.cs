using ControlHub.Application.Roles.Commands.AssignRoleToUser;
using ControlHub.Application.Roles.Queries.GetUserRoles;
using ControlHub.API.Controllers; // BaseApiController
using ControlHub.API.Roles.ViewModels.Requests;
using ControlHub.API.Roles.ViewModels.Responses;
using ControlHub.Application.Common.DTOs;
using ControlHub.Application.Roles.Commands.CreateRoles;
using ControlHub.Application.Roles.Commands.SetRolePermissions;
using ControlHub.Application.Roles.Queries.GetRolePermissions;
using ControlHub.Application.Roles.Queries.SearchRoles;
using ControlHub.Domain.Roles;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlHub.API.Roles
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoleController : BaseApiController
    {
        private readonly ILogger<RoleController> _logger;

        public RoleController(IMediator mediator, ILogger<RoleController> logger) : base(mediator, logger)
        {
            _logger = logger;
        }

        [HttpPost("users/{userId}/assign/{roleId}")]
        [Authorize(Policy = "Permission:roles.assign")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignRoleToUser(Guid userId, Guid roleId, CancellationToken cancellationToken)
        {
            var command = new AssignRoleToUserCommand(userId, roleId);
            var result = await Mediator.Send(command, cancellationToken);

            if (result.IsFailure) return HandleFailure(result);

            return Ok();
        }

        [HttpGet("users/{userId}")]
        [Authorize(Policy = "Permission:roles.view")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserRoles(Guid userId, CancellationToken cancellationToken)
        {
            var query = new GetUserRolesQuery(userId);
            var result = await Mediator.Send(query, cancellationToken);
            
            if (result.IsFailure) return HandleFailure(result);

            return Ok(result.Value);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "Permission:roles.update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
        {
            // Note: Request body doesn't contain ID, but Command does.
            var command = new ControlHub.Application.Roles.Commands.UpdateRole.UpdateRoleCommand(id, request.Name, request.Description);
            var result = await Mediator.Send(command, cancellationToken);

            if (result.IsFailure) return HandleFailure(result);

            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "Permission:roles.delete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
        {
            var command = new ControlHub.Application.Roles.Commands.DeleteRole.DeleteRoleCommand(id);
            var result = await Mediator.Send(command, cancellationToken);

            if (result.IsFailure) return HandleFailure(result);

            return NoContent();
        }

        [Authorize(Policy = "Permission:roles.view")]
        [HttpGet("{roleId}/permissions")]
        [ProducesResponseType(typeof(List<ControlHub.Application.Permissions.DTOs.PermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRolePermissions(Guid roleId, CancellationToken cancellationToken)
        {
            var query = new GetRolePermissionsQuery(roleId);
            var result = await Mediator.Send(query, cancellationToken);
            if (result.IsFailure) return HandleFailure(result);
            return Ok(result.Value);
        }

        [Authorize(Policy = "Permission:roles.update")]
        [HttpPut("{roleId}/permissions")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetRolePermissions(Guid roleId, [FromBody] List<Guid> permissionIds, CancellationToken cancellationToken)
        {
            var command = new SetRolePermissionsCommand(roleId, permissionIds);
            var result = await Mediator.Send(command, cancellationToken);
            if (result.IsFailure) return HandleFailure(result);
            return NoContent();
        }



        [Authorize(Policy = "Permission:roles.create")]
        [HttpPost("roles")]
        [ProducesResponseType(typeof(CreateRolesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateRoles([FromBody] CreateRolesRequest request, CancellationToken ct)
        {
            var command = new CreateRolesCommand(request.Roles);
            var result = await Mediator.Send(command, ct);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            if (result is Result<PartialResult<Role, string>> typedResult)
            {
                var summary = typedResult.Value;
                return Ok(new CreateRolesResponse
                {
                    Message = summary.Failures.Any()
                        ? "Partial success: some roles failed to create."
                        : "All roles created successfully.",
                    SuccessCount = summary.Successes.Count,
                    FailureCount = summary.Failures.Count,
                    FailedRoles = summary.Failures
                });
            }

            return Ok(new CreateRolesResponse
            {
                Message = "All roles created successfully.",
                SuccessCount = request.Roles.Count(),
                FailureCount = 0
            });
        }

        [Authorize(Policy = "Permission:roles.view")]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<Role>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRoles(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null)
        {
            var conditions = string.IsNullOrEmpty(searchTerm) ? Array.Empty<string>() : new[] { searchTerm };

            var query = new SearchRolesQuery(pageIndex, pageSize, conditions);
            var result = await Mediator.Send(query);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return Ok(result.Value);
        }
    }
}
