using System.Net;
using System.Net.Http.Json;
using ControlHub.Api.Tests.Abstractions;
using ControlHub.Application.Accounts.Commands.CreateIdentifier;
using ControlHub.Application.Accounts.DTOs;
using ControlHub.Domain.Identity.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ControlHub.Api.Tests.FunctionalTests.Identifiers;

public class IdentifierTests : BaseIntegrationTest
{
    public IdentifierTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateIdentifierConfig_ShouldReturnCreated_WhenAuthorizedAndValid()
    {
        // Arrange
        var identifier = $"test_ident_creator_{Guid.NewGuid()}@example.com";
        await AuthenticateAsync(identifier, "Password@123");

        // Grant "identifiers.create" permission
        var account = await DbContext.Accounts.Include(a => a.Role).FirstAsync(a => a.Identifiers.Any(i => i.Value == identifier));
        var perm = await DbContext.Permissions.FirstAsync(p => p.Code == "identifiers.create");
        account.Role.AddPermission(perm);
        await DbContext.SaveChangesAsync();

        var rules = new List<ValidationRuleDto>
        {
            new ValidationRuleDto(ValidationRuleType.Required, new Dictionary<string, object>())
        };

        var command = new CreateIdentifierConfigCommand("NewConfig", "Description", rules);

        // Act
        var response = await Client.PostAsJsonAsync("/api/Identifier", command);

        // Assert
        if (response.StatusCode != HttpStatusCode.Created)
        {
             var content = await response.Content.ReadAsStringAsync();
             response.StatusCode.Should().Be(HttpStatusCode.Created, $"Fail. Content: {content}");
        }
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateIdentifierConfig_ShouldReturnConflict_WhenNameExists()
    {
        // Arrange
        await AuthenticateAsSuperAdminAsync();
        
        var rules = new List<ValidationRuleDto>
        {
            new ValidationRuleDto(ValidationRuleType.Required, new Dictionary<string, object>())
        };
        
        // First create
        var command1 = new CreateIdentifierConfigCommand("DuplicateConfig", "Desc", rules);
        var response1 = await Client.PostAsJsonAsync("/api/Identifier", command1);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Second create
        var response2 = await Client.PostAsJsonAsync("/api/Identifier", command1);

        // Assert
        if (response2.StatusCode != HttpStatusCode.Conflict)
        {
             var content = await response2.Content.ReadAsStringAsync();
             response2.StatusCode.Should().Be(HttpStatusCode.Conflict, $"Fail. Content: {content}");
        }
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetActiveIdentifierConfigs_ShouldReturnOk_WhenAnonymous()
    {
        // Act
        var response = await Client.GetAsync("/api/Identifier/active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetActiveIdentifierConfigs_ShouldIgnoreIncludeDeactivated_WhenAnonymous()
    {
        // Arrange: valid super admin setup to create a deactivated config
        await AuthenticateAsSuperAdminAsync();
        var rules = new List<ValidationRuleDto>
        {
            new ValidationRuleDto(ValidationRuleType.Required, new Dictionary<string, object>())
        };
        var command = new CreateIdentifierConfigCommand("DeactivatedConfig", "Hidden", rules);
        var createResponse = await Client.PostAsJsonAsync("/api/Identifier", command);
        var createdId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        
        // Deactivate it
        await Client.PatchAsJsonAsync($"/api/Identifier/{createdId}/toggle-active", new { IsActive = false });
        
        // Logout / Reset Client to anonymous
        Client.DefaultRequestHeaders.Authorization = null;

        // Act: Try to get it with includeDeactivated=true
        var response = await Client.GetAsync("/api/Identifier/active?includeDeactivated=true");
        var configs = await response.Content.ReadFromJsonAsync<List<IdentifierConfigDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // It SHOULD NOT contain the deactivated one.
        configs.Should().NotContain(c => c.Name == "DeactivatedConfig", "Anonymous users should not be able to see deactivated configs even with the flag.");
    }

    [Fact]
    public async Task ToggleIdentifierActive_ShouldReturnOk_WhenAuthorized()
    {
        // Arrange
        var identifier = $"test_toggler_{Guid.NewGuid()}@example.com";
        await AuthenticateAsync(identifier, "Password@123");

        // Grant "identifiers.toggle"
        var account = await DbContext.Accounts.Include(a => a.Role).FirstAsync(a => a.Identifiers.Any(i => i.Value == identifier));
        var perm = await DbContext.Permissions.FirstAsync(p => p.Code == "identifiers.toggle");
        account.Role.AddPermission(perm);
        
        // Ensure a config exists
        var config = await DbContext.IdentifierConfigs.FirstAsync();
        
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.PatchAsJsonAsync($"/api/Identifier/{config.Id}/toggle-active", new { IsActive = false });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
