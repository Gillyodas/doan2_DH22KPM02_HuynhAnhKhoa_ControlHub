using ControlHub.API.Accounts.ViewModels.Request;
using ControlHub.API.Accounts.ViewModels.Response;
using ControlHub.Application.Accounts.Commands.CreateAccount;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ControlHub.API.Accounts.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var command = new CreateAccountCommand(request.Email, request.Password);

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return BadRequest(new RegisterResponse { Message = result.Error });

            return Ok(new RegisterResponse
            {
                AccountId = result.Value,
                Message = "Register success"
            });
        }
    }
}
