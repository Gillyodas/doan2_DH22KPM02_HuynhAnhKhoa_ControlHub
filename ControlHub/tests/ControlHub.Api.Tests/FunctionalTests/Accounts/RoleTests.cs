using ControlHub.Api.Tests.Abstractions;
using ControlHub.API.Roles.ViewModels.Requests;
using ControlHub.API.Roles.ViewModels.Responses;
using ControlHub.Application.Roles.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

using Microsoft.EntityFrameworkCore;

namespace ControlHub.Api.Tests.FunctionalTests.Accounts;

public class RoleTests : BaseIntegrationTest
{
    public RoleTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateRoles_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        await AuthenticateAsSuperAdminAsync();
        var permissionId = (await DbContext.Set<ControlHub.Domain.Permissions.Permission>().FirstAsync()).Id;

        var request = new CreateRolesRequest
        {
            Roles = new List<CreateRoleDto>
            {
                new CreateRoleDto 
                ( 
                    "Test Role " + Guid.NewGuid().ToString().Substring(0, 8), 
                    "Test Description",
                    new List<Guid> { permissionId } 
                )
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Role/roles", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CreateRolesResponse>();
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateRoles_ShouldReturnBadRequest_WhenNameIsEmpty()
    {
        // This is a "BUG HUNTING" test. 
        // We expect it to fail if validation works, but currently we expect it 
        // to pass (return 200) because the internal validator is bypassed!
        
        // Arrange
        await AuthenticateAsSuperAdminAsync();
        var permissionId = (await DbContext.Set<ControlHub.Domain.Permissions.Permission>().FirstAsync()).Id;

        var request = new CreateRolesRequest
        {
            Roles = new List<CreateRoleDto>
            {
                new CreateRoleDto 
                ( 
                    "", // Invalid: Empty Name
                    "No Name Role",
                    new List<Guid> { permissionId }
                )
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Role/roles", request);

        // Assert
        // IF THIS TEST PASSES WITH OK, THEN WE HAVE A BUG.
        // I will assert it is OK to PROVE the bug exists, then I will change it back or fix the bug.
        // Actually, let's assert it is BadRequest to catch the regression after fix.
        // But for now, I want to see it fail with "Expected BadRequest but got OK".
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Validation should have caught the empty name");
    }

    [Fact]
    public async Task CreateRoles_ShouldReturnForbidden_WhenNotSuperAdmin()
    {
        // Arrange
        await AuthenticateAsync(); // Regular User

        var request = new CreateRolesRequest
        {
            Roles = new List<CreateRoleDto>
            {
                new CreateRoleDto ("AdminRole", "Desc", new List<Guid>())
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Role/roles", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateRoles_ShouldReturnOk_WithPartialSuccess_WhenOneRoleIsInvalid()
    {
        // Arrange
        await AuthenticateAsSuperAdminAsync();
        var permissionId = (await DbContext.Set<ControlHub.Domain.Permissions.Permission>().FirstAsync()).Id;

        var request = new CreateRolesRequest
        {
            Roles = new List<CreateRoleDto>
            {
                new CreateRoleDto("Valid Role " + Guid.NewGuid().ToString().Substring(0,8), "Desc", new List<Guid> { permissionId }),
                new CreateRoleDto("Handler-Level Failure", "Desc", new List<Guid> { Guid.NewGuid() }) // Random GUID - Not found in DB
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Role/roles", request);

        // Assert
        // The handler is designed to return OK if AT LEAST one succeeds, but with details in response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CreateRolesResponse>();
        result.Should().NotBeNull();
        result.FailedRoles.Should().Contain(f => f.Contains("Permission.NotFound"));
    }

    [Fact]
    public async Task CreateRoles_ShouldReturnOk_AndCorrectlyReportDuplicates_WhenRoleAlreadyExists()
    {
        // This is a test for the fix of the "silent filtering" bug.
        // Arrange
        await AuthenticateAsSuperAdminAsync();
        var permissionId = (await DbContext.Set<ControlHub.Domain.Permissions.Permission>().FirstAsync()).Id;
        string duplicateName = "Duplicate Role " + Guid.NewGuid().ToString().Substring(0, 8);

        // First creation
        await Client.PostAsJsonAsync("/api/Role/roles", new CreateRolesRequest
        {
            Roles = new List<CreateRoleDto> { new CreateRoleDto(duplicateName, "Desc", new List<Guid> { permissionId }) }
        });

        // Second creation (Duplicate + New)
        var request = new CreateRolesRequest
        {
            Roles = new List<CreateRoleDto>
            {
                new CreateRoleDto("New Unique Role", "Desc", new List<Guid> { permissionId }),
                new CreateRoleDto(duplicateName, "Desc", new List<Guid> { permissionId }) // Duplicate
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Role/roles", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CreateRolesResponse>();
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(1);
        
        // BUG: Currently, the duplicate is silently filtered out. 
        // We might want to assert that FailureCount is 1 if we consider this a bug,
        // or just observe it. Currently, it will be 0 because it's filtered out from validDtos list.
        result.FailureCount.Should().Be(1, "The duplicate role should be reported as a failure, not silently ignored.");
    }
}
