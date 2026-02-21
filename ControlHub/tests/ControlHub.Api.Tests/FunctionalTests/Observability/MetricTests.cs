using System.Net;
using ControlHub.Api.Tests.Abstractions;
using FluentAssertions;

namespace ControlHub.Api.Tests.FunctionalTests.Observability;

public class MetricTests : BaseIntegrationTest
{
    public MetricTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Metrics_ShouldReturnOk()
    {
        // Act
        var response = await Client.GetAsync("/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
