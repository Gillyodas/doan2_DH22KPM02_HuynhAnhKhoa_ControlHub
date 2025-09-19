using ControlHub.API.Accounts.ViewModels.Request;
using ControlHub.API.Accounts.ViewModels.Response;
using ControlHub.Application.Accounts.Commands.ChangePassword;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ControlHub.API.Accounts.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class accountController : ControllerBase
    {
        private readonly IMediator _mediator;

        public accountController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("{id}/change-password")]
        public async Task<IActionResult> ChangePasswordCommand(Guid id, [FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            var command = new ChangePasswordCommand(id, request.curPass, request.newPass);

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return BadRequest(new ChangePasswordResponse { Message = result.Error });

            return Ok();
        }
    }
}
