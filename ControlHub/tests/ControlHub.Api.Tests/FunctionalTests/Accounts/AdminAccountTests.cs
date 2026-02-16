using ControlHub.Api.Tests.Abstractions;
using ControlHub.API.Accounts.ViewModels.Request;
using ControlHub.API.Accounts.ViewModels.Response;
using ControlHub.Domain.Identity.Enums;
using ControlHub.Application.Accounts.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using System.Collections.Generic;

namespace ControlHub.Api.Tests.FunctionalTests.Accounts;

public class AdminAccountTests : BaseIntegrationTest
{
    public AdminAccountTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAdmins_ShouldReturnList_WhenAuthorizedAsSuperAdmin()
    {
        // 1. Register SuperAdmin
        var superAdminEmail = $"superadmin_{Guid.NewGuid()}@example.com";
        var password = "StrongPassword123!";
        var masterKey = "MasterKey"; // From appsettings.json in test (hopefully routed correctly)

        var regResponse = await Client.PostAsJsonAsync("/api/Auth/superadmins/register", new RegisterSupperAdminRequest
        {
            Value = superAdminEmail,
            Password = password,
            Type = IdentifierType.Email,
            MasterKey = masterKey
        });
        regResponse.EnsureSuccessStatusCode();

        // 2. Login as SuperAdmin
        var loginResponse = await Client.PostAsJsonAsync("/api/Auth/auth/signin", new SignInRequest
        {
            Value = superAdminEmail,
            Password = password,
            Type = IdentifierType.Email
        });
        var tokens = await loginResponse.Content.ReadFromJsonAsync<SignInResponse>();
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        // 3. Register an Admin (using CreateAdmin endpoint if available, or just verify empty list first)
        // Since we are SuperAdmin, we can call POST api/Auth/admins/register
        var adminEmail = $"admin_{Guid.NewGuid()}@example.com";
        var adminRegResponse = await Client.PostAsJsonAsync("/api/Auth/admins/register", new RegisterAdminRequest
        {
            Value = adminEmail,
            Password = password,
            Type = IdentifierType.Email
        });
        adminRegResponse.EnsureSuccessStatusCode();

        // 4. Get Admins
        var getResponse = await Client.GetAsync("/api/Account/admins");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var admins = await getResponse.Content.ReadFromJsonAsync<List<AccountDto>>();
        admins.Should().NotBeNull();
        admins.Should().Contain(a => a.Username == adminEmail || a.Username.Contains(adminEmail)); // Identifier value is returned as Username
    }
}
