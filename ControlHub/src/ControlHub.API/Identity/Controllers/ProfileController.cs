using ControlHub.API.Extensions;
using ControlHub.API.Identity.ViewModels.Request;
using ControlHub.Application.Identity.Commands.ChangePassword;
using ControlHub.Application.Common.Interfaces;
using ControlHub.Application.Identity.Commands.UpdateMyProfile;
using ControlHub.Application.Identity.Queries.GetMyProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ControlHub.API.Identity.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting(RateLimitingExtensions.Policies.GeneralApi)]
    public class ProfileController : ControlHub.API.Controllers.BaseApiController
    {
        private readonly ICurrentUserService _currentUserService;

        public ProfileController(IMediator mediator, ILogger<ProfileController> logger, ICurrentUserService currentUserService)
            : base(mediator, logger)
        {
            _currentUserService = currentUserService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
        {
            var query = new GetMyProfileQuery();
            var result = await Mediator.Send(query, cancellationToken);
            return HandleFailure(result) ?? Ok(result.Value);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateMyProfileCommand(request.FirstName, request.LastName, request.PhoneNumber);
            var result = await Mediator.Send(command, cancellationToken);
            return HandleFailure(result) ?? NoContent();
        }

        [HttpPut("me/password")]
        public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
            var result = await Mediator.Send(command, cancellationToken);
            return HandleFailure(result) ?? NoContent();
        }
    }
}
