using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PrakApp.Api.Tests.Common;

// Fehlender Test: Lese-Adressen antworten mit 404, wenn es den Kunden bzw. Vertrag mit dieser Id nicht gibt.
// Wie die anderen Endpunkt-Tests mit der normalen Datenbank; der Test liest nur, er ändert nichts.
// Falle: Auch eine falsch geschriebene Adresse liefert 404 (Auffang-Regel für /api/... in Program.cs, siehe
// UnknownApiRouteTests). Deshalb wird jede Adresse zuerst mit einer Id aufgerufen, die es gibt (200). Das beweist,
// dass die Adresse stimmt und die 404 danach wirklich vom Controller kommt
public class UnknownIdEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    // {0} wird durch die Id ersetzt. Vorhandene Ids aus den Demo-Daten: Kundin 64 (Clara Quast) mit Vertrag 77 (aktiv)
    [Theory]
    [InlineData("/api/customers/{0}", 64)]
    [InlineData("/api/customers/{0}/contracts", 64)]
    [InlineData("/api/contracts/{0}", 77)]
    [InlineData("/api/contracts/{0}/payments", 77)]
    [InlineData("/api/contracts/{0}/cancellation-preview", 77)]
    public async Task Unknown_id_returns_404(string url, int existingId)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new { userName = "praktikum" })).EnsureSuccessStatusCode();

        // Gegencheck mit einer vorhandenen Id: Die Adresse gibt es
        var existing = await client.GetAsync(string.Format(url, existingId));
        Assert.Equal(HttpStatusCode.OK, existing.StatusCode);

        // Dieselbe Adresse mit einer Id, die es nicht gibt
        var unknown = await client.GetAsync(string.Format(url, 999999));
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }
}
