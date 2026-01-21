using System.Net;
using System.Net.Http.Json;
using ControlHub.Api.Tests.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ControlHub.Api.Tests.FunctionalTests.Users;

public class UserTests : BaseIntegrationTest
{
    public UserTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task UpdateUsername_ShouldReturnOk_WhenAuthorized()
    {
        // Arrange
        var email = $"userupdate_{Guid.NewGuid()}@example.com";
        await AuthenticateAsync(email, "Password@123");

        // Grant "users.update_username" permission
        var account = await DbContext.Accounts.Include(a => a.Role).Include(a => a.User).FirstAsync(a => a.Identifiers.Any(i => i.Value == email));
        var perm = await DbContext.Permissions.FirstAsync(p => p.Code == "users.update_username");
        account.Role.AddPermission(perm);
        await DbContext.SaveChangesAsync();

        var newUsername = "SuperUserUpdated";

        // Act
        // NOTE: The handler uses GetByAccountId, so we must pass AccountId, not UserId.
        var response = await Client.PatchAsJsonAsync($"/api/User/users/{account.Id}/username", new { Username = newUsername });

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
            {
             var content = await response.Content.ReadAsStringAsync();
             response.StatusCode.Should().Be(HttpStatusCode.OK, $"Fail. Content: {content}");
            }
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify in DB - Reload to ensure we get fresh data
        DbContext.ChangeTracker.Clear();
        var updatedUser = await DbContext.Users.FindAsync(account.User.Id);
        updatedUser!.Username.Should().Be(newUsername);
    }
}
