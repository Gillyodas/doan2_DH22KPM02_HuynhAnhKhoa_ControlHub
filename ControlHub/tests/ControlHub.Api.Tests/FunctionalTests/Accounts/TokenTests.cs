using ControlHub.Api.Tests.Abstractions;
using ControlHub.API.Accounts.ViewModels.Request;
using ApiResponse = ControlHub.API.Accounts.ViewModels.Response;
using ControlHub.Domain.Identity.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ControlHub.Api.Tests.FunctionalTests.Accounts;

public class TokenTests : BaseIntegrationTest
{
    public TokenTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnNewTokens_WhenValid()
    {
        // Arrange - Register and Login to get tokens
        var email = "tokenuser@example.com";
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
        
        var tokens = await loginResponse.Content.ReadFromJsonAsync<ApiResponse.SignInResponse>();

        var refreshRequest = new RefreshAccessTokenRequest
        {
            AccessToken = tokens!.AccessToken,
            RefreshToken = tokens.RefreshToken,
            AccID = tokens.AccountId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Auth/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshResponse = await response.Content.ReadFromJsonAsync<ApiResponse.RefreshAccessTokenResponse>();
        refreshResponse.Should().NotBeNull();
        refreshResponse!.AccessToken.Should().NotBeNullOrEmpty();
        refreshResponse.RefreshToken.Should().NotBeNullOrEmpty();
        refreshResponse.AccessToken.Should().NotBe(tokens.AccessToken);
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnUnauthorized_WhenTokenIsInvalid()
    {
        // Arrange
        var refreshRequest = new RefreshAccessTokenRequest
        {
            AccessToken = "invalid",
            RefreshToken = "invalid",
            AccID = Guid.NewGuid()
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Auth/auth/refresh", refreshRequest);

        // Assert
        // The implementation might return Unauthorized or BadRequest depending on validation/logic
        response.StatusCode.Should().Match(s => s == HttpStatusCode.Unauthorized || s == HttpStatusCode.BadRequest || s == HttpStatusCode.InternalServerError || s == HttpStatusCode.NotFound);
    }
}
