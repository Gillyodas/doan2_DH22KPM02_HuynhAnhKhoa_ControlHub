using System.Net;
using ControlHub.Api.Tests.Abstractions;
using FluentAssertions;

namespace ControlHub.Api.Tests.FunctionalTests.ApiExplorer;

public class SwaggerTests : BaseIntegrationTest
{
    public SwaggerTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SwaggerJson_ShouldReturnOk()
    {
        // Act
        var response = await Client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SwaggerUi_ShouldReturnOk()
    {
        // Act
        var response = await Client.GetAsync("/swagger/index.html");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
