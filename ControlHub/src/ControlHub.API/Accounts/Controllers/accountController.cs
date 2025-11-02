using ControlHub.API.Accounts.Mappers;
using ControlHub.API.Accounts.ViewModels.Request;
using ControlHub.API.Accounts.ViewModels.Response;
using ControlHub.Application.Accounts.Commands.ChangePassword;
using ControlHub.Application.Accounts.Commands.ResetPassword;
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

        [HttpPost("change-password/{id}")]
        public async Task<IActionResult> ChangePasswordCommand(Guid id, [FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            var command = new ChangePasswordCommand(id, request.curPass, request.newPass);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new ChangePasswordResponse { Message = result.Error.Message });

            return Ok();
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            var command = ForgotPasswordRequestMapper.ToCommand(request);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new ForgotPasswordResponse { Message = result.Error.Message });

            return Ok();
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            var command = new ResetPasswordCommand(request.Token, request.Password);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new ResetPasswordResponse { Message = result.Error.Message });

            return Ok();
        }
    }
}
