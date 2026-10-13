using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PrakApp.Api.Auth;

namespace PrakApp.Api.Tests.Auth;

/// <summary>
/// Startet die API im Speicher (ohne Datenbankzugriff) und prüft die Demo-Anmeldung.
/// </summary>
public class AuthEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Api_requires_login()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Demo_users_are_listed()
    {
        var client = factory.CreateClient();

        var users = await client.GetFromJsonAsync<List<AuthController.UserInfo>>("/api/auth/users");

        Assert.NotNull(users);
        Assert.Contains(users, u => u.UserName == "praktikum");
    }

    [Fact]
    public async Task Login_with_unknown_user_is_rejected()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName = "niemand" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_sets_cookie_and_me_returns_user()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var login = await client.PostAsJsonAsync("/api/auth/login", new { userName = "m.berger" });
        login.EnsureSuccessStatusCode();
        var me = await client.GetFromJsonAsync<AuthController.UserInfo>("/api/auth/me");

        Assert.Equal("Marta Berger", me?.DisplayName);
    }
}
