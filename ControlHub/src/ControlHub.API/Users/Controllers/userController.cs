using ControlHub.API.Accounts.ViewModels.Response;
using ControlHub.API.Users.ViewModels.Request;
using ControlHub.API.Users.ViewModels.Response;
using ControlHub.Application.Users.Commands.UpdateUsername;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ControlHub.API.Users.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class userController : Controller
    {
        private readonly IMediator _mediator;
        public userController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPatch("{id}/username")]
        public async Task<IActionResult> UpdateUsername(Guid id, [FromBody] UpdateUsernameRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateUsernameCommand(id, request.username);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new UpdateUsernameResponse { message = result.Error });

            return Ok(new UpdateUsernameResponse { username = result.Value });
        }
    }
}