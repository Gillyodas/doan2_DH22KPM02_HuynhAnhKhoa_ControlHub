using System.Net;
using System.Net.Http.Json;
using ControlHub.Api.Tests.Abstractions;
using ControlHub.API.Accounts.ViewModels.Request;
using ControlHub.Domain.Identity.Enums;
using ControlHub.Domain.TokenManagement.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ControlHub.Api.Tests.FunctionalTests.Accounts;

public class AccountTests : BaseIntegrationTest
{
    public AccountTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnNoContent_WhenAuthorized()
    {
        // Arrange
        var email = $"changepass_{Guid.NewGuid()}@example.com";
        var oldPass = "Password@123";
        var newPass = "NewPassword@123";
        await AuthenticateAsync(email, oldPass);

        var account = await DbContext.Accounts.FirstAsync(a => a.Identifiers.Any(i => i.Value == email));

        // Act
        var response = await Client.PatchAsJsonAsync($"/api/Account/users/{account.Id}/password", new ChangePasswordRequest
        {
            curPass = oldPass,
            newPass = newPass
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify login with new password
        await Client.PostAsJsonAsync("/api/Auth/auth/signout", new { }); // Logout first (optional but good practice)
        var loginResponse = await Client.PostAsJsonAsync("/api/Auth/auth/signin", new { Value = email, Password = newPass, Type = IdentifierType.Email });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnForbidden_WhenUpdatingOtherUser()
    {
        // Arrange
        var hackerEmail = $"hacker_{Guid.NewGuid()}@example.com";
        var victimEmail = $"victim_{Guid.NewGuid()}@example.com";

        await AuthenticateAsync(hackerEmail, "Password@123");

        // Create victim by just registering them (no login needed)
        await Client.PostAsJsonAsync("/api/Auth/users/register", new { Value = victimEmail, Password = "Password@123", Type = IdentifierType.Email });
        var victimAccount = await DbContext.Accounts.FirstAsync(a => a.Identifiers.Any(i => i.Value == victimEmail));

        // Act
        var response = await Client.PatchAsJsonAsync($"/api/Account/users/{victimAccount.Id}/password", new ChangePasswordRequest
        {
            curPass = "Password@123", // Even if they know the password
            newPass = "HackedPassword@123"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnOk_WhenEmailExists()
    {
        // Arrange
        var email = $"forgot_{Guid.NewGuid()}@example.com";
        await AuthenticateAsync(email, "Password@123");
        // We are authenticated just to create the user easily, but ForgotPassword is anonymous.
        // Clear headers
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.PostAsJsonAsync("/api/Account/auth/forgot-password", new { Value = email, Type = IdentifierType.Email });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnOk_WhenTokenIsValid()
    {
        // Arrange
        var email = $"reset_{Guid.NewGuid()}@example.com";
        await AuthenticateAsync(email, "OldPassword@123");
        Client.DefaultRequestHeaders.Authorization = null; // Reset is anonymous

        // 1. Trigger Forgot Password
        await Client.PostAsJsonAsync("/api/Account/auth/forgot-password", new { Value = email, Type = IdentifierType.Email });

        // 2. Retrieve the Token from DB (Backdoor)
        var account = await DbContext.Accounts.Include(a => a.User).FirstAsync(a => a.Identifiers.Any(i => i.Value == email));
        // Look for the latest ResetPassword token for this account
        var token = await DbContext.Tokens
            .Where(t => t.AccountId == account.Id && t.Type == TokenType.ResetPassword)
            .OrderByDescending(t => t.ExpiredAt)
            .FirstOrDefaultAsync();

        token.Should().NotBeNull("Token should be generated and stored in DB.");

        // Act
        var response = await Client.PostAsJsonAsync("/api/Account/auth/reset-password", new { Token = token!.Value, Password = "NewStrongPassword@123" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify can login with new password
        var loginResponse = await Client.PostAsJsonAsync("/api/Auth/auth/signin", new { Value = email, Password = "NewStrongPassword@123", Type = IdentifierType.Email });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
