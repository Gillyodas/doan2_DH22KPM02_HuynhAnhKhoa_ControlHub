using ControlHub.API.Controllers;
using ControlHub.API.Permissions.ViewModels.Requests;
using ControlHub.Application.Common.DTOs;
using ControlHub.Application.Permissions.Commands.CreatePermissions;
using ControlHub.Application.Permissions.Queries.SearchPermissions;
using ControlHub.Domain.AccessControl.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlHub.API.Permissions
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionController : BaseApiController
    {
        private readonly ILogger<PermissionController> _logger;

        public PermissionController(IMediator mediator, ILogger<PermissionController> logger) : base(mediator, logger)
        {
            _logger = logger;
        }

        [Authorize(Policy = "Permission:permissions.create")]
        [HttpPost("permissions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreatePermissions([FromBody] CreatePermissionsRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("--- DEBUG: PermissionController.CreatePermissions HIT ---");
            var command = new CreatePermissionsCommand(request.Permissions);

            var result = await Mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return Ok();
        }

        [Authorize(Policy = "Permission:permissions.view")]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<Permission>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPermissions(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            var conditions = string.IsNullOrEmpty(searchTerm) ? Array.Empty<string>() : new[] { searchTerm };

            var query = new SearchPermissionsQuery(pageIndex, pageSize, conditions);

            var result = await Mediator.Send(query);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return Ok(result.Value);
        }

        [Authorize(Policy = "Permission:permissions.update")]
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePermission(Guid id, [FromBody] UpdatePermissionRequest request, CancellationToken cancellationToken)
        {
            var command = new ControlHub.Application.Permissions.Commands.UpdatePermission.UpdatePermissionCommand(id, request.Code, request.Description);
            var result = await Mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return NoContent();
        }

        [Authorize(Policy = "Permission:permissions.delete")]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePermission(Guid id, CancellationToken cancellationToken)
        {
            var command = new ControlHub.Application.Permissions.Commands.DeletePermission.DeletePermissionCommand(id);
            var result = await Mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return NoContent();
        }
    }
}
