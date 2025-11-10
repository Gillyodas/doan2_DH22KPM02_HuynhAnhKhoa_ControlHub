using ControlHub.API.Accounts.Mappers;
using ControlHub.API.Accounts.ViewModels.Request;
using ControlHub.API.Accounts.ViewModels.Response;
using ControlHub.Application.Accounts.Commands.RefreshAccessToken;
using ControlHub.Application.Accounts.Commands.SignOut;
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
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            var command = RegisterRequestMapper.ToCommand(request);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new RegisterResponse { Message = result.Error.Message });

            return Ok(new RegisterResponse
            {
                AccountId = result.Value,
                Message = "Register success"
            });
        }

        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] SignInRequest request, CancellationToken cancellationToken)
        {
            var command = SignInRequestMapper.ToCommand(request);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new SignInResponse { message = result.Error.Message });

            return Ok(new SignInResponse
            {
                accountId = result.Value.AccountId,
                username = result.Value.Username,
                accessToken = result.Value.AccessToken,
                refreshToken = result.Value.RefreshToken
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshAccessTokenRequest request, CancellationToken cancellationToken)
        {
            var command = new RefreshAccessTokenCommand(request.RefreshToken, request.AccID, request.AccessToken);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new SignInResponse { message = result.Error.Message });

            return Ok(new RefreshAccessTokenReponse
            {
                RefreshToken = request.RefreshToken,
                AccessToken = request.AccessToken
            });
        }

        [HttpPost("signout")]
        public async Task<IActionResult> SignOut([FromBody] SignOutRequest request, CancellationToken cancellationToken)
        {
            var command = new SignOutCommand(request.accessToken, request.refreshToken);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new SignOutResponse { message = result.Error.Message });

            return Ok(new SignOutResponse
            {
                message = null
            });
        }
    }
}
