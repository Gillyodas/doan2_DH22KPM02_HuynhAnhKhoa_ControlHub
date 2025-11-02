using ControlHub.API.Permissions.ViewModels.Requests;
using ControlHub.API.Permissions.ViewModels.Responses;
using ControlHub.Application.Permissions.Commands.CreatePermissions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ControlHub.API.Permissions
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionController : ControllerBase
    {
        private readonly IMediator _mediator;
        public PermissionController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPost("permissions")]
        public async Task<IActionResult> CreatePermissions([FromBody] CreatePermissionsRequest request)
        {
            var command = new CreatePermissionsCommand(request.Permissions);

            var result = await _mediator.Send(command);

            if(result.IsFailure)
            {
                return BadRequest(new CreatePermissionsResponse { Message = result.Error.Message });
            }

            return Ok();
        }
    }
}
