using ControlHub.API.Accounts.ViewModels.Request;
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
using ApiResponse = ControlHub.API.Accounts.ViewModels.Response;

namespace ControlHub.API.Accounts.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : BaseApiController
    {
        private ILogger<AuthController> _logger;
        // Constructor truy?n Mediator xu?ng l?p Base
        public AuthController(IMediator mediator, ILogger<AuthController> logger) : base(mediator, logger)
        {
        }

        [AllowAnonymous]
        [HttpPost("users/register")]
        [ProducesResponseType(typeof(ApiResponse.RegisterUserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken ct)
        {
            var command = new RegisterUserCommand(request.Value, request.Type, request.Password, request.IdentifierConfigId);
            var result = await Mediator.Send(command, ct);

            if (result.IsFailure)
                return HandleFailure(result); // S? d?ng hàm t? BaseApiController

            return Ok(new ApiResponse.RegisterUserResponse
            {
                AccountId = result.Value,
                Message = "Register success"
            });
        }

        [Authorize(Policy = "Permission:users.create")]
        [HttpPost("admins/register")]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterAdminRequest request, CancellationToken ct)
        {
            var command = new RegisterAdminCommand(request.Value, request.Type, request.Password, request.IdentifierConfigId);
            var result = await Mediator.Send(command, ct);

            if (result.IsFailure)
                return HandleFailure(result);

            return Ok(new ApiResponse.RegisterAdminResponse
            {
                AccountId = result.Value,
                Message = "Admin registration success"
            });
        }

        [AllowAnonymous]
        [HttpPost("superadmins/register")]
        public async Task<IActionResult> RegisterSuperAdmin([FromBody] RegisterSupperAdminRequest request, CancellationToken ct)
        {
            var command = new RegisterSupperAdminCommand(request.Value, request.Type, request.Password, request.MasterKey, request.IdentifierConfigId);
            var result = await Mediator.Send(command, ct);

            if (result.IsFailure)
                return HandleFailure(result);

            return Ok(new ApiResponse.RegisterSupperAdminResponse
            {
                AccountId = result.Value,
                Message = "SuperAdmin registration success"
            });
        }

        [AllowAnonymous]
        [HttpPost("auth/signin")]
        [ProducesResponseType(typeof(ApiResponse.SignInResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SignIn([FromBody] SignInRequest request, CancellationToken ct)
        {
            var command = new SignInCommand(request.Value, request.Password, request.Type, request.IdentifierConfigId);
            var result = await Mediator.Send(command, ct);

            if (result.IsFailure)
                return HandleFailure(result);

            return Ok(new ApiResponse.SignInResponse
            {
                AccountId = result.Value.AccountId,
                Username = result.Value.Username,
                AccessToken = result.Value.AccessToken,
                RefreshToken = result.Value.RefreshToken
            });
        }

        [AllowAnonymous]
        [HttpPost("auth/refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshAccessTokenRequest request, CancellationToken ct)
        {
            var command = new RefreshAccessTokenCommand(request.RefreshToken, request.AccID, request.AccessToken);
            var result = await Mediator.Send(command, ct);

            if (result.IsFailure)
                return HandleFailure(result);

            return Ok(new ApiResponse.RefreshAccessTokenResponse
            {
                RefreshToken = result.Value.RefreshToken,
                AccessToken = result.Value.AccessToken
            });
        }

        [Authorize]
        [HttpPost("auth/signout")]
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
