using ControlHub.Api.Tests.Abstractions;
using ControlHub.API.Accounts.ViewModels.Request;
using ControlHub.API.Accounts.ViewModels.Response;
using ControlHub.Domain.Accounts.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ControlHub.Api.Tests.FunctionalTests.Accounts;

public class LoginTests : BaseIntegrationTest
{
    public LoginTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_ShouldReturnOk_WhenCredentialsAreValid()
    {
        // Arrange - Register a user first
        var email = "loginuser@example.com";
        var password = "StrongPassword123!";
        
        await Client.PostAsJsonAsync("/api/Auth/users/register", new RegisterUserRequest
        {
            Value = email,
            Password = password,
            Type = IdentifierType.Email
        });

        var loginRequest = new SignInRequest
        {
            Value = email,
            Password = password,
            Type = IdentifierType.Email
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Auth/auth/signin", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResponse = await response.Content.ReadFromJsonAsync<SignInResponse>();
        loginResponse.Should().NotBeNull();
        loginResponse!.AccessToken.Should().NotBeNullOrEmpty();
        loginResponse.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordIsWrong()
    {
        // Arrange
        var email = "wrongpass@example.com";
        await Client.PostAsJsonAsync("/api/Auth/users/register", new RegisterUserRequest
        {
            Value = email,
            Password = "StrongPassword123!",
            Type = IdentifierType.Email
        });

        var loginRequest = new SignInRequest
        {
            Value = email,
            Password = "WrongPassword123!",
            Type = IdentifierType.Email
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Auth/auth/signin", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenPasswordIsTooShort()
    {
        // Arrange
        var loginRequest = new SignInRequest
        {
            Value = "any@example.com",
            Password = "123",
            Type = IdentifierType.Email
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Auth/auth/signin", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
