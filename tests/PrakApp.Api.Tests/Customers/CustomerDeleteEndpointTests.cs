using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PrakApp.Api.Tests.Customers;

// Verbesserung: Kunden löschen (DELETE /api/customers/{id}), laut api.md nur Kunden ohne Verträge.
// Wie die anderen Endpunkt-Tests mit der normalen Datenbank. Diese Tests löschen nichts: Das erfolgreiche Löschen
// prüft der Playwright-Test e2e/customer-delete.spec.ts mit einem Kunden, den er selbst anlegt
public class CustomerDeleteEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private async Task<HttpClient> LoginAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new { userName = "praktikum" })).EnsureSuccessStatusCode();
        return client;
    }

    [Fact]
    public async Task Customer_with_contracts_is_not_deleted()
    {
        var client = await LoginAsync();

        // Kundin 64 (Clara Quast) aus den Demo-Daten hat Vertrag 77
        var response = await client.DeleteAsync("/api/customers/64");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Kunden mit Verträgen können nicht gelöscht werden.", problem!.Title);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/customers/64")).StatusCode);
    }

    [Fact]
    public async Task Unknown_customer_is_not_found()
    {
        var client = await LoginAsync();

        var response = await client.DeleteAsync("/api/customers/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
