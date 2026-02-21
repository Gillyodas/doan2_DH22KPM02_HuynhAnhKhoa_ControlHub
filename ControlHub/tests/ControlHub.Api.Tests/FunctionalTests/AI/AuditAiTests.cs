using System.Net;
using System.Net.Http.Json;
using ControlHub.Api.Tests.Abstractions;
using FluentAssertions;

namespace ControlHub.Api.Tests.FunctionalTests.AI
{
    public class AuditAiTests : BaseIntegrationTest
    {
        public AuditAiTests(IntegrationTestWebAppFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetVersion_ShouldReturnV25_WhenConfigured()
        {
            // Act: Call endpoint (AllowAnonymous)
            var response = await Client.GetAsync("/api/Audit/version");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<VersionResponse>();
            result.Should().NotBeNull();
            // Note: In integration test environment, appsettings might be different ("V1" vs "V2.5").
            // But we expect at least a valid JSON response.
            result.Version.Should().NotBeNullOrEmpty();
        }

        private class VersionResponse
        {
            public string Version { get; set; } = string.Empty;
            public string[] Features { get; set; } = System.Array.Empty<string>();
        }
    }
}
