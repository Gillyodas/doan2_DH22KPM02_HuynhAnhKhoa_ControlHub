using ControlHub.API.Accounts.ViewModels.Request;
using ControlHub.API.Controllers;
using ControlHub.Application.Accounts.Commands.ChangePassword;
using ControlHub.Application.Accounts.Commands.ForgotPassword;
using ControlHub.Application.Accounts.Commands.ResetPassword;
using ControlHub.Application.Accounts.Queries.GetAdminAccounts;
using ControlHub.Application.Authorization.Requirements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlHub.API.Accounts.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : BaseApiController
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
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            // Resource-based Authorization: Ki?m tra quy?n trên tài nguyên c? th? (id)
            var authResult = await _authorizationService.AuthorizeAsync(User, id, new SameUserRequirement());

            if (!authResult.Succeeded)
            {
                return Forbid(); // Tr? v? 403 Forbidden chu?n xác
            }

            var command = new ChangePasswordCommand(id, request.curPass, request.newPass);

            var result = await Mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return HandleFailure(result); // T? d?ng map l?i (VD: Sai pass cu -> 400, Account Deleted -> 400)
            }

            return NoContent(); // Thành công và không có n?i dung tr? v?
        }

        [AllowAnonymous]
        [HttpPost("auth/forgot-password")]
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
