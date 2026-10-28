using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PrakApp.Api.Tests.Common;

// Bug: Für eine API-Adresse, die es nicht gibt, lieferte der Server die Startseite der App (index.html, Status 200)
// statt "nicht gefunden" (404). Grund war der Fallback MapFallbackToFile in Program.cs, der jede unbekannte Adresse
// auffängt. Jetzt gibt es dort zusätzlich einen Fallback nur für /api/..., der 404 liefert.
// Wie in den anderen Endpunkt-Tests startet WebApplicationFactory<Program> die komplette API im Speicher.
// Der Test braucht keine Datenbank: Unbekannte Adressen kommen gar nicht bis zu einem Repository.
public class UnknownApiRouteTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    // Der Test läuft dreimal, je mit HTTP-Methode und Adresse aus einer [InlineData]-Zeile
    [Theory]
    // Adresse, zu der es keinen Controller gibt (vorher: 200 mit der HTML-Startseite)
    [InlineData("GET", "/api/gibt-es-nicht")]
    // Den Kunden-Controller gibt es, aber die Id muss eine Zahl sein ({id:int}), "abc" passt also zu keiner Route
    // (vorher: 200 mit der HTML-Startseite)
    [InlineData("GET", "/api/customers/abc")]
    // Dieselbe unbekannte Adresse mit POST (vorher: 405 "Methode nicht erlaubt", weil der alte Fallback nur GET annimmt)
    [InlineData("POST", "/api/gibt-es-nicht")]
    public async Task Unknown_api_address_returns_404(string method, string url)
    {
        // Anmelden wie das Frontend im Alltag. Ohne Anmeldung würden bekannte API-Adressen mit 401 antworten;
        // so ist sicher, dass der Test den Fall prüft, der in der App wirklich vorkommt
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new { userName = "praktikum" })).EnsureSuccessStatusCode();

        // SendAsync statt GetAsync, damit derselbe Test GET und POST schicken kann:
        // new HttpMethod("GET") bzw. new HttpMethod("POST") macht aus dem Text die passende HTTP-Methode
        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        // Erwartet: 404 Not Found
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        // Die Antwort ist JSON im Format ProblemDetails wie bei den anderen Fehlern der API, keine HTML-Seite.
        // MediaType ist der Typ ohne Zusatz wie "; charset=utf-8"; das ?. liefert null, falls gar kein Typ gesetzt ist
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
