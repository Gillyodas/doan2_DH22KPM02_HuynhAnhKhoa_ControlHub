using ControlHub.API.Accounts.ViewModels.Request;
using ControlHub.API.Accounts.ViewModels.Response;
using ControlHub.API.Controllers; // Import BaseApiController
using ControlHub.Application.Accounts.Commands.CreateAccount;
using ControlHub.Application.Accounts.Commands.RefreshAccessToken;
using ControlHub.Application.Accounts.Commands.RegisterAdmin;
using ControlHub.Application.Accounts.Commands.RegisterSupperAdmin;
using ControlHub.Application.Accounts.Commands.SignIn;
using ControlHub.Application.Accounts.Commands.SignOut;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlHub.API.Accounts.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : BaseApiController
    {
        // Constructor truyền Mediator xuống lớp Base
        public AuthController(IMediator mediator) : base(mediator)
        {
        }

        [AllowAnonymous]
        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken ct)
        {
            var command = new RegisterUserCommand(request.Value, request.Type, request.Password);
            var result = await Mediator.Send(command, ct);

            if (result.IsFailure)
                return HandleFailure(result); // Sử dụng hàm từ BaseApiController

            return Ok(new RegisterUserResponse
            {
                AccountId = result.Value,
                Message = "Register success"
            });
        }

        [Authorize(Policy = "Permission:account.register_admin")]
        [HttpPost("register-admin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterAdminRequest request, CancellationToken ct)
        {
            var command = new RegisterAdminCommand(request.Value, request.Type, request.Password);
            var result = await Mediator.Send(command, ct);

            if (result.IsFailure)
                return HandleFailure(result);

            return Ok(new RegisterAdminResponse
            {
                AccountId = result.Value,
                Message = "Admin registration success"
            });
        }

        [AllowAnonymous]
        [HttpPost("register-superadmin")]
        public async Task<IActionResult> RegisterSuperAdmin([FromBody] RegisterSupperAdminRequest request, CancellationToken ct)
        {
            var command = new RegisterSupperAdminCommand(request.Value, request.Type, request.Password, request.MasterKey);
            var result = await Mediator.Send(command, ct);

            if (result.IsFailure)
                return HandleFailure(result);

            return Ok(new RegisterSupperAdminResponse
            {
                AccountId = result.Value,
                Message = "SuperAdmin registration success"
            });
        }

        [AllowAnonymous]
        [HttpPost("signin")]
        [ProducesResponseType(typeof(SignInResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SignIn([FromBody] SignInRequest request, CancellationToken ct)
        {
            var command = new SignInCommand(request.Value, request.Password, request.Type);
            var result = await Mediator.Send(command, ct);

            if (result.IsFailure)
                return HandleFailure(result);

            return Ok(new SignInResponse
            {
                AccountId = result.Value.AccountId,
                Username = result.Value.Username,
                AccessToken = result.Value.AccessToken,
                RefreshToken = result.Value.RefreshToken
            });
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshAccessTokenRequest request, CancellationToken ct)
        {
            var command = new RefreshAccessTokenCommand(request.RefreshToken, request.AccID, request.AccessToken);
            var result = await Mediator.Send(command, ct);

            if (result.IsFailure)
                return HandleFailure(result);

            return Ok(new RefreshAccessTokenReponse
            {
                RefreshToken = result.Value.RefreshToken,
                AccessToken = result.Value.AccessToken
            });
        }

        [Authorize]
        [HttpPost("signout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SignOut([FromBody] SignOutRequest request, CancellationToken ct)
        {
            var command = new SignOutCommand(request.accessToken, request.refreshToken);
            var result = await Mediator.Send(command, ct);

            if (result.IsFailure)
                return HandleFailure(result);

            return NoContent();
        }
    }
}