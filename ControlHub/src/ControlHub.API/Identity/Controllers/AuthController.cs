using ControlHub.API.Extensions;
using ControlHub.API.Identity.ViewModels.Request;
using ControlHub.Application.Identity.Commands.RegisterUser;
using ControlHub.Application.Identity.Commands.RefreshAccessToken;
using ControlHub.Application.Identity.Commands.RegisterAdmin;
using ControlHub.Application.Identity.Commands.RegisterSupperAdmin;
using ControlHub.Application.Identity.Commands.SignIn;
using ControlHub.Application.Identity.Commands.SignOut;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ApiResponse = ControlHub.API.Identity.ViewModels.Response;

namespace ControlHub.API.Identity.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControlHub.API.Controllers.BaseApiController
    {
        private ILogger<AuthController> _logger;

        public AuthController(IMediator mediator, ILogger<AuthController> logger) : base(mediator, logger)
        {
        }

        [AllowAnonymous]
        [HttpPost("users/register")]
        [EnableRateLimiting(RateLimitingExtensions.Policies.Authentication)]
        [ProducesResponseType(typeof(ApiResponse.RegisterUserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken ct)
        {
            var command = new RegisterUserCommand(request.Value, request.Type, request.Password, request.IdentifierConfigId);
            var result = await Mediator.Send(command, ct);

            if (result.IsFailure)
                return HandleFailure(result);

            return Ok(new ApiResponse.RegisterUserResponse
            {
                AccountId = result.Value,
                Message = "Register success"
            });
        }

        [Authorize(Policy = "Permission:users.create")]
        [HttpPost("admins/register")]
        [EnableRateLimiting(RateLimitingExtensions.Policies.Authentication)]
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
        [EnableRateLimiting(RateLimitingExtensions.Policies.Authentication)]
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
        [EnableRateLimiting(RateLimitingExtensions.Policies.Authentication)]
        [ProducesResponseType(typeof(ApiResponse.SignInResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SignIn([FromBody] SignInRequest request, CancellationToken ct)
        {
            var command = new SignInCommand(request.Value, request.Password);
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
            var command = new SignOutCommand(request.AccessToken, request.RefreshToken);
            var result = await Mediator.Send(command, ct);

            if (result.IsFailure)
                return HandleFailure(result);

            return NoContent();
        }
    }
}
