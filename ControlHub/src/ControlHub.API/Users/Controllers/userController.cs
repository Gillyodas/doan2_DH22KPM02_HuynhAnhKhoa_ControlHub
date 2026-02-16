using ControlHub.API.Controllers;
using ControlHub.API.Users.ViewModels.Request;
using ControlHub.API.Users.ViewModels.Response;
using ControlHub.Application.Users.Commands.UpdateUsername;
using ControlHub.Application.Users.Commands.UpdateUser;
using ControlHub.Application.Users.Commands.DeleteUser;
using ControlHub.Application.Users.Queries.GetUsers;
using ControlHub.Application.Users.Queries.GetUserById;
using ControlHub.Application.Users.Queries.GetUserById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlHub.API.Users.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : BaseApiController
    {
        private readonly ILogger<UserController> _logger;

        public UserController(IMediator mediator, ILogger<UserController> logger) : base(mediator, logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "Permission:users.view")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUsersQuery(page, pageSize, searchTerm);
            var result = await Mediator.Send(query, cancellationToken);
            
            if (result.IsFailure) return HandleFailure(result);

            return Ok(result.Value);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "Permission:users.view")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetUserByIdQuery(id);
            var result = await Mediator.Send(query, cancellationToken);

            if (result.IsFailure) return HandleFailure(result);

            return Ok(result.Value);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "Permission:users.update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        {
            if (id != request.Id) return BadRequest("Id mismatch");
            
            var command = new UpdateUserCommand(
                id, 
                request.Email, 
                request.FirstName, 
                request.LastName, 
                request.PhoneNumber, 
                request.IsActive);
                
            var result = await Mediator.Send(command, cancellationToken);

            if (result.IsFailure) return HandleFailure(result);

            return Ok(result.Value);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "Permission:users.delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteUserCommand(id);
            var result = await Mediator.Send(command, cancellationToken);

            if (result.IsFailure) return HandleFailure(result);

            return Ok();
        }

        [HttpPatch("users/{id}/username")]
        [Authorize(Policy = "Permission:users.update_username")]
        [ProducesResponseType(typeof(UpdateUsernameResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateUsername(Guid id, [FromBody] UpdateUsernameRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateUsernameCommand(id, request.Username);

            var result = await Mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return Ok(new UpdateUsernameResponse { Username = result.Value });
        }
    }
}
