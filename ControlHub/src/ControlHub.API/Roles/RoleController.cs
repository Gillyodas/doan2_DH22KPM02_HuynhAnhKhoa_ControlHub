using ControlHub.API.Controllers; // BaseApiController
using ControlHub.API.Roles.ViewModels.Requests;
using ControlHub.API.Roles.ViewModels.Responses;
using ControlHub.Application.Common.DTOs;
using ControlHub.Application.Roles.Commands.CreateRoles;
using ControlHub.Application.Roles.Commands.SetRolePermissions; // Đổi tên namespace nếu bạn đã đổi tên command thành AddPermissionsForRole
using ControlHub.Application.Roles.Queries.SearchRoles;
using ControlHub.Domain.Permissions;
using ControlHub.Domain.Roles;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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

        [Authorize(Policy = "Permission:role.add_permissions")]
        [HttpPost("roles/{roleId}/permissions")]
        [ProducesResponseType(typeof(AddPermissonsForRoleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddPermissionsForRole(string roleId, [FromBody] AddPermissonsForRoleRequest request, CancellationToken cancellationToken)
        {
            var command = new AddPermissonsForRoleCommand(roleId, request.PermissionIds, cancellationToken);
            var result = await Mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            // Xử lý Partial Result
            if (result is Result<PartialResult<Permission, string>> typedResult)
            {
                var summary = typedResult.Value;
                return Ok(new AddPermissonsForRoleResponse
                {
                    Message = summary.Failures.Any()
                        ? "Partial success: some permissions failed to add."
                        : "All permissions added successfully.",
                    SuccessCount = summary.Successes.Count, // Count thay vì Count()
                    FailureCount = summary.Failures.Count,
                    FailedRoles = summary.Failures // Kiểm tra lại tên property trong Response DTO, có thể nên là FailedPermissions
                });
            }

            // Fallback
            return Ok(new AddPermissonsForRoleResponse
            {
                Message = "Permissions updated successfully.",
                SuccessCount = request.PermissionIds.Count(),
                FailureCount = 0
            });
        }

        [Authorize(Policy = "Permission:role.create")]
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

        [Authorize(Policy = "Permission:role.view")]
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