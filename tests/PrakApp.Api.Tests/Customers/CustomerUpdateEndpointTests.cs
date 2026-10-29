using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PrakApp.Api.Tests.Customers;

// Fehlender Test: Kunde ändern (PUT /api/customers/{id}) mit falscher Eingabe (400) und für einen unbekannten Kunden (404).
// Wie die anderen Endpunkt-Tests mit der normalen Datenbank. Beide Anfragen werden abgelehnt, es wird also nichts geändert.
// Das erfolgreiche Speichern prüft der Playwright-Test "Bearbeiten → Speichern ..." in e2e/customers.spec.ts
public class CustomerUpdateEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private async Task<HttpClient> LoginAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new { userName = "praktikum" })).EnsureSuccessStatusCode();
        return client;
    }

    [Fact]
    public async Task Update_with_invalid_input_returns_400_with_field_error()
    {
        var client = await LoginAsync();

        // Kunde 1 aus den Demo-Daten, aber ohne Nachnamen: Der Validator lehnt ab, bevor etwas gespeichert wird
        var response = await client.PutAsJsonAsync("/api/customers/1", new { firstName = "Anna", lastName = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["Nachname ist erforderlich."], problem!.Errors["LastName"]);
    }

    [Fact]
    public async Task Update_of_unknown_customer_returns_404()
    {
        var client = await LoginAsync();

        // Gültige Eingabe, aber einen Kunden mit Id 999999 gibt es nicht
        var response = await client.PutAsJsonAsync("/api/customers/999999", new { firstName = "Anna", lastName = "Albers" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
