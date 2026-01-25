using ControlHub.API.Controllers;
using ControlHub.API.Permissions.ViewModels.Requests;
using ControlHub.Application.Common.DTOs;
using ControlHub.Application.Permissions.Commands.CreatePermissions;
using ControlHub.Application.Permissions.Queries.SearchPermissions;
using ControlHub.Domain.Permissions;
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

        [Authorize(Policy = Policies.CanCreatePermission)]
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

        [Authorize(Policy = Policies.CanViewPermissions)]
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
    }
}