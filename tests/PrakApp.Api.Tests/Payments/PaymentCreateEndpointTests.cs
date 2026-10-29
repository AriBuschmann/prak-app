using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PrakApp.Api.Tests.Payments;

// Fehlender Test: Zahlung für einen Vertrag, den es nicht gibt (POST /api/contracts/{id}/payments), wird mit 404 abgelehnt.
// Wie die anderen Endpunkt-Tests mit der normalen Datenbank. Die Anfrage wird abgelehnt, es wird keine Zahlung gespeichert
public class PaymentCreateEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Payment_for_unknown_contract_returns_404()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new { userName = "praktikum" })).EnsureSuccessStatusCode();

        // Gültige Zahlung, aber einen Vertrag mit Id 999999 gibt es nicht
        var response = await client.PostAsJsonAsync("/api/contracts/999999/payments",
            new { amount = 24.90m, paymentDate = "2026-10-01", reference = "Beitrag 10/2026" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
