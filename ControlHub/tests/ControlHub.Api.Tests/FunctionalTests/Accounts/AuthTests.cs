using ControlHub.Api.Tests.Abstractions;
using ControlHub.API.Accounts.ViewModels.Request;
using ControlHub.API.Accounts.ViewModels.Response;
using ControlHub.Domain.Identity.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ControlHub.Api.Tests.FunctionalTests.Accounts;

public class AuthTests : BaseIntegrationTest
{
    public AuthTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SignOut_ShouldReturnNoContent_WhenTokensAreValid()
    {
        // Arrange
        var email = "signoutuser@example.com";
        var password = "StrongPassword123!";
        
        await Client.PostAsJsonAsync("/api/Auth/users/register", new RegisterUserRequest
        {
            Value = email,
            Password = password,
            Type = IdentifierType.Email
        });

        var loginResponse = await Client.PostAsJsonAsync("/api/Auth/auth/signin", new SignInRequest
        {
            Value = email,
            Password = password,
            Type = IdentifierType.Email
        });
        
        var tokens = await loginResponse.Content.ReadFromJsonAsync<SignInResponse>();
        
        // Authorize for the SignOut request
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var signOutRequest = new SignOutRequest
        {
            accessToken = tokens.AccessToken,
            refreshToken = tokens.RefreshToken
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Auth/auth/signout", signOutRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SignOut_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var signOutRequest = new SignOutRequest
        {
            accessToken = "any",
            refreshToken = "any"
        };
        
        // Ensure no auth header
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.PostAsJsonAsync("/api/Auth/auth/signout", signOutRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
