using System.Net;
using System.Net.Http.Json;
using ControlHub.API.Permissions.ViewModels.Requests;
using ControlHub.Application.Permissions.Commands.CreatePermissions;
using ControlHub.Application.Permissions.DTOs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ControlHub.Api.Tests.Abstractions;
using Xunit;

namespace ControlHub.Api.Tests.FunctionalTests.Permissions;

public class PermissionTests : BaseIntegrationTest
{
    public PermissionTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreatePermissions_ShouldReturnOk_WhenUserHasCorrectPluralPermission()
    {
        // This verifies the fix for the singular/plural naming inconsistency.
        
        // Arrange
        var identifier = $"test_perm_success_{Guid.NewGuid()}@example.com";
        await AuthenticateAsync(identifier, "Password@123");
        
        // Give this user "permissions.create" (plural)
        var account = await DbContext.Accounts.Include(a => a.Role).FirstAsync(a => a.Identifiers.Any(i => i.Value == identifier));
        var pluralPermission = await DbContext.Permissions.FirstAsync(p => p.Code == "permissions.create");
        account.Role.AddPermission(pluralPermission);
        await DbContext.SaveChangesAsync();
        
        var request = new CreatePermissionsRequest
        {
            Permissions = new[]
            {
                new CreatePermissionDto($"test.{Guid.NewGuid():N}", "Test Description")
            }
        };
        
        // Act
        var response = await Client.PostAsJsonAsync("/api/Permission/permissions", request);
        
        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Fail. Content: {content}");
        }
        
        response.StatusCode.Should().Be(HttpStatusCode.OK, "User with plural permission should now match plural policy.");
    }

    [Fact]
    public async Task CreatePermissions_ShouldReturnBadRequest_WhenCodeIsEmpty()
    {
        // This tests if the validator for CreatePermissionDto is discovered.
        // It was internal, now it is public.
        
        // Arrange
        await AuthenticateAsSuperAdminAsync();
        var request = new CreatePermissionsRequest
        {
            Permissions = new[]
            {
                new CreatePermissionDto("", "Description")
            }
        };
        
        // Act
        var response = await Client.PostAsJsonAsync("/api/Permission/permissions", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Validation should fail for empty permission code.");
    }

    [Fact]
    public async Task CreatePermissions_ShouldReturnConflict_WhenPermissionAlreadyExists()
    {
        // Arrange
        await AuthenticateAsSuperAdminAsync();
        var existingCode = (await DbContext.Permissions.FirstAsync()).Code;
        
        var request = new CreatePermissionsRequest
        {
            Permissions = new[]
            {
                new CreatePermissionDto(existingCode, "Duplicate")
            }
        };
        
        // Act
        var response = await Client.PostAsJsonAsync("/api/Permission/permissions", request);
        
        // Assert
        // PermisisonCommandHandler returns Result.Failure(PermissionErrors.PermissionCodeAlreadyExists)
        // which HandleFailure transforms to 409 Conflict.
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreatePermissions_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var request = new CreatePermissionsRequest
        {
            Permissions = new[]
            {
                new CreatePermissionDto("test.permission", "Test Description")
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Permission/permissions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePermissions_ShouldFailTransactional_WhenOneIsInvalid()
    {
        // Tests partial success logic / atomicity.
        // We send 1 valid permission and 1 invalid permission.
        // The expectation is that the entire batch fails (Transactional) or validation stops it.
        // And the valid permission should NOT be in the DB.

        // Arrange
        await AuthenticateAsSuperAdminAsync();
        var validCode = $"valid.perm.{Guid.NewGuid():N}";
        
        var request = new CreatePermissionsRequest
        {
            Permissions = new[]
            {
                new CreatePermissionDto(validCode, "Valid Description"),
                new CreatePermissionDto("", "Invalid Code") // Should trigger validation failure
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Permission/permissions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Verify Atomicity: validCode should NOT exist
        var exists = await DbContext.Permissions.AnyAsync(p => p.Code == validCode);
        exists.Should().BeFalse("Batch creation should be atomic; valid permission should not be created if batch fails.");
    }
}
