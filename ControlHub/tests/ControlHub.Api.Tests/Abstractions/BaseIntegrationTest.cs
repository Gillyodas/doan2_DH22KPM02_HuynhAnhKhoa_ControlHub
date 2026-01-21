using ControlHub.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using ApiResponse = ControlHub.API.Accounts.ViewModels.Response;

namespace ControlHub.Api.Tests.Abstractions;

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IServiceScope _scope;
    protected readonly IntegrationTestWebAppFactory Factory;
    protected readonly HttpClient Client;
    protected readonly AppDbContext DbContext;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        
        _scope = factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    protected async Task AuthenticateAsync(string email = "test@example.com", string password = "StrongPassword123!")
    {
        // Register (ignore if already exists)
        var regResponse = await Client.PostAsJsonAsync("/api/Auth/users/register", new
        {
            Value = email,
            Password = password,
            Type = 0 // Email
        });

        var response = await Client.PostAsJsonAsync("/api/Auth/auth/signin", new
        {
            Value = email,
            Password = password,
            Type = 0
        });

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new System.Exception($"Authentication failed for {email}: Status {response.StatusCode}, Error: {error}");
        }

        var loginResponse = await response.Content.ReadFromJsonAsync<ApiResponse.SignInResponse>();
        string token = loginResponse!.AccessToken;

        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    protected async Task AuthenticateAsSuperAdminAsync(string email = "superadmin@example.com", string password = "SuperPassword123!")
    {
        // Register SuperAdmin using MasterKey
        var regResponse = await Client.PostAsJsonAsync("/api/Auth/superadmins/register", new
        {
            Value = email,
            Password = password,
            Type = 0, // Email
            MasterKey = "MasterKey"
        });

        var response = await Client.PostAsJsonAsync("/api/Auth/auth/signin", new
        {
            Value = email,
            Password = password,
            Type = 0
        });

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new System.Exception($"SuperAdmin login failed for {email}: Status {response.StatusCode}, Error: {error}");
        }

        var loginResponse = await response.Content.ReadFromJsonAsync<ApiResponse.SignInResponse>();
        string token = loginResponse!.AccessToken;

        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}
