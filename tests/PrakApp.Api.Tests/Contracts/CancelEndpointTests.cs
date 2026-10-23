using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PrakApp.Api.Tests.Contracts;

// WebApplicationFactory<Program> startet die komplette API (wie dotnet run), aber nur im Speicher ohne echten Port.
// IClassFixture sorgt dafür, dass sie für alle Tests dieser Klasse nur einmal gestartet wird.
public class CancelEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Cancel_without_reason_returns_400_with_field_error()
    {
        // Ein HTTP-Client wie ein Browser; HandleCookies = true merkt sich das Anmelde-Cookie für die nächsten Aufrufe
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        // Anmelden, denn ohne Anmeldung käme 401 statt 400. EnsureSuccessStatusCode bricht ab, falls das schiefgeht
        (await client.PostAsJsonAsync("/api/auth/login", new { userName = "praktikum" })).EnsureSuccessStatusCode();

        // Vertrag 1 kündigen, aber nur mit Leerzeichen als Grund, das muss der CancellationValidator ablehnen
        var response = await client.PostAsJsonAsync("/api/contracts/1/cancel", new { reason = "   " });

        // Erwartet: 400 Bad Request (falsche Eingabe), nicht 204 (gekündigt) oder 409 (Vertrag nicht aktiv)
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // Die Antwort ist ValidationProblemDetails im JSON-Format, darin stehen die Fehler pro Feld
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        // Der Fehler muss beim Feld "Reason" stehen, damit das Frontend ihn am Eingabefeld anzeigen kann
        Assert.Contains("Reason", problem!.Errors.Keys);
    }
}
