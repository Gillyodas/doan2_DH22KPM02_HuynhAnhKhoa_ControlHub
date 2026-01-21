using ControlHub.Api.Tests.Abstractions;
using ControlHub.API.Accounts.ViewModels.Request;
using ControlHub.Domain.Accounts.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ControlHub.Api.Tests.FunctionalTests.Accounts;

public class RegistrationTests : BaseIntegrationTest
{
    public RegistrationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Register_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Value = "newuser@example.com",
            Password = "StrongPassword123!",
            Type = IdentifierType.Email,
            IdentifierConfigId = null // Assuming default or not needed
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Auth/users/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_ShouldReturnConflict_WhenUserAlreadyExists()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Value = "duplicate@example.com",
            Password = "StrongPassword123!",
            Type = IdentifierType.Email
        };

        // Act - First Register
        var firstResponse = await Client.PostAsJsonAsync("/api/Auth/users/register", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Second Register (Duplicate)
        var secondResponse = await Client.PostAsJsonAsync("/api/Auth/users/register", request);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordIsTooShort()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Value = "badpass@example.com",
            Password = "123",
            Type = IdentifierType.Email
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Auth/users/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
