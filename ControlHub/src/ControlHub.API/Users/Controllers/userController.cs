using ControlHub.API.Controllers;
using ControlHub.API.Users.ViewModels.Request;
using ControlHub.API.Users.ViewModels.Response;
using ControlHub.Application.Users.Commands.UpdateUsername;
using ControlHub.Domain.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlHub.API.Users.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : BaseApiController
    {
        private readonly ILogger<UserController> _logger;

        public UserController(IMediator mediator, ILogger<UserController> logger) : base(mediator, logger)
        {
            _logger = logger;
        }

        [HttpPatch("users/{id}/username")]
        [ProducesResponseType(typeof(UpdateUsernameResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateUsername(Guid id, [FromBody] UpdateUsernameRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateUsernameCommand(id, request.Username);

            var result = await Mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return Ok(new UpdateUsernameResponse { Username = result.Value });
        }
    }
}