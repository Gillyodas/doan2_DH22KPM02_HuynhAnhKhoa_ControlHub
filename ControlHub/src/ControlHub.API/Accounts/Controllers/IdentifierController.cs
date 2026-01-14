using ControlHub.Application.Accounts.Commands.CreateIdentifier;
using ControlHub.Application.Accounts.Commands.ToggleIdentifierActive;
using ControlHub.Application.Accounts.Commands.UpdateIdentifierConfig;
using ControlHub.Application.Accounts.DTOs;
using ControlHub.Application.Accounts.Queries.GetActiveIdentifierConfigs;
using ControlHub.Application.Accounts.Queries.GetIdentifierConfigs;
using ControlHub.API.Controllers;
using ControlHub.Domain.Permissions;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlHub.API.Accounts.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class IdentifierController : BaseApiController
    {
        private readonly ILogger<IdentifierController> _logger;

        public IdentifierController(IMediator mediator, ILogger<IdentifierController> logger) 
            : base(mediator, logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get all identifier configurations
        /// </summary>
        /// <returns>List of identifier configurations</returns>
        [HttpGet]
        [Authorize(Policy = "Permission:identifiers.view")]
        [ProducesResponseType(typeof(List<IdentifierConfigDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetIdentifierConfigs()
        {
            _logger.LogInformation("Getting all identifier configurations");

            var query = new GetIdentifierConfigsQuery();
            var result = await Mediator.Send(query);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to get identifier configurations: {Error}", result.Error);
                return HandleFailure(Result.Failure(result.Error));
            }

            _logger.LogInformation("Successfully retrieved {Count} identifier configurations", 
                result.Value?.Count ?? 0);
            return Ok(result.Value);
        }

        /// <summary>
        /// Create a new identifier configuration
        /// </summary>
        /// <param name="command">Identifier configuration creation request</param>
        /// <returns>Created identifier configuration ID</returns>
        [HttpPost]
        [Authorize(Policy = "Permission:identifiers.create")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateIdentifierConfig([FromBody] CreateIdentifierConfigCommand command)
        {
            _logger.LogInformation("Creating identifier configuration with name: {Name}", command.Name);

            var result = await Mediator.Send(command);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to create identifier configuration: {Error}", result.Error);
                return HandleFailure(Result.Failure(result.Error));
            }

            _logger.LogInformation("Successfully created identifier configuration with ID: {Id}", result.Value);
            return CreatedAtAction(nameof(GetIdentifierConfigs), new { }, result.Value);
        }

        /// <summary>
        /// Get active identifier configurations (for login page)
        /// </summary>
        /// <param name="includeDeactivated">Whether to include deactivated configurations</param>
        /// <returns>List of active identifier configurations</returns>
        [HttpGet("active")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<IdentifierConfigDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetActiveIdentifierConfigs([FromQuery] bool includeDeactivated = false)
        {
            _logger.LogInformation("Getting active identifier configurations, includeDeactivated: {IncludeDeactivated}", includeDeactivated);

            var query = new GetActiveIdentifierConfigsQuery(includeDeactivated);
            var result = await Mediator.Send(query);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to get active identifier configurations: {Error}", result.Error);
                return HandleFailure(Result.Failure(result.Error));
            }

            var configType = includeDeactivated ? "all" : "active";
            _logger.LogInformation("Successfully retrieved {Count} {ConfigType} identifier configurations", 
                result.Value?.Count ?? 0, configType);
            return Ok(result.Value);
        }

        /// <summary>
        /// Toggle identifier configuration active status
        /// </summary>
        /// <param name="id">Identifier configuration ID</param>
        /// <param name="request">Toggle request with isActive flag</param>
        /// <returns>Success result</returns>
        [HttpPatch("{id}/toggle-active")]
        [Authorize(Policy = "Permission:identifiers.toggle")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ToggleIdentifierActive(Guid id, [FromBody] ToggleActiveRequest request)
        {
            _logger.LogInformation("Toggling identifier configuration {Id} to {IsActive}", id, request.IsActive);

            var command = new ToggleIdentifierActiveCommand(id, request.IsActive);
            var result = await Mediator.Send(command);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to toggle identifier configuration: {Error}", result.Error);
                return HandleFailure(result);
            }

            _logger.LogInformation("Successfully toggled identifier configuration {Id}", id);
            return Ok();
        }

        /// <summary>
        /// Update identifier configuration
        /// </summary>
        /// <param name="id">Identifier configuration ID</param>
        /// <param name="command">Update request</param>
        /// <returns>Success result</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "Permission:identifiers.update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateIdentifierConfig(Guid id, [FromBody] UpdateIdentifierConfigCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest("ID mismatch");
            }

            _logger.LogInformation("Updating identifier configuration {Id}", id);

            var result = await Mediator.Send(command);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to update identifier configuration: {Error}", result.Error);
                return HandleFailure(result);
            }

            _logger.LogInformation("Successfully updated identifier configuration {Id}", id);
            return Ok();
        }
    }

    public record ToggleActiveRequest(bool IsActive);
}
