using ControlHub.API.Extensions;
using ControlHub.API.Identity.ViewModels.Request;
using ControlHub.Application.Identity.Authorization;
using ControlHub.Application.Identity.Commands.ChangePassword;
using ControlHub.Application.Identity.Commands.ForgotPassword;
using ControlHub.Application.Identity.Commands.ResetPassword;
using ControlHub.Application.Identity.Queries.GetAdminAccounts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ControlHub.API.Identity.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class AccountController : ControlHub.API.Controllers.BaseApiController
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IMediator mediator, IAuthorizationService authorizationService, ILogger<AccountController> logger)
            : base(mediator, logger)
        {
            _authorizationService = authorizationService;
            _logger = logger;
        }

        [Authorize]
        [HttpPatch("users/{id}/password")]
        [EnableRateLimiting(RateLimitingExtensions.Policies.Sensitive)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            var authResult = await _authorizationService.AuthorizeAsync(User, id, new SameUserRequirement());

            if (!authResult.Succeeded)
            {
                return Forbid();
            }

            var command = new ChangePasswordCommand(id, request.CurrentPassword, request.NewPassword);

            var result = await Mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return NoContent();
        }

        [AllowAnonymous]
        [HttpPost("auth/forgot-password")]
        [EnableRateLimiting(RateLimitingExtensions.Policies.Sensitive)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            var command = new ForgotPasswordCommand(request.Value, request.Type);

            var result = await Mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("auth/reset-password")]
        [EnableRateLimiting(RateLimitingExtensions.Policies.Sensitive)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            var command = new ResetPasswordCommand(request.Token, request.Password);

            var result = await Mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return Ok();
        }

        [Authorize(Policy = "Permission:users.view")]
        [HttpGet("admins")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAdmins(CancellationToken cancellationToken)
        {
            var query = new GetAdminAccountsQuery();
            var result = await Mediator.Send(query, cancellationToken);
            return HandleFailure(result) ?? Ok(result.Value);
        }
    }
}
