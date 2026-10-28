using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PrakApp.Api.Tests.Customers;

// Wie CancelEndpointTests: WebApplicationFactory<Program> startet die komplette API im Speicher (ohne echten Port),
// IClassFixture startet sie für alle Tests dieser Klasse nur einmal. Die API arbeitet mit der normalen Datenbank PrakApp.
public class CustomerEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    // Bug: "Angelegt am" zeigte nachts (0 bis 2 Uhr) das Datum vom Vortag. CreatedAt ist in der Datenbank UTC,
    // wurde aber ohne Zeitzone verschickt ("2023-03-28T10:15:00"), und der Browser hat es als Ortszeit gelesen.
    // Mit "Z" am Ende (Z = UTC) weiß der Browser, dass er in seine Ortszeit umrechnen muss.
    [Fact]
    public async Task Created_at_is_sent_as_utc()
    {
        // HTTP-Client wie ein Browser; HandleCookies = true merkt sich das Anmelde-Cookie für den nächsten Aufruf
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        // Anmelden, sonst antwortet die API mit 401. EnsureSuccessStatusCode bricht ab, falls die Anmeldung scheitert
        (await client.PostAsJsonAsync("/api/auth/login", new { userName = "praktikum" })).EnsureSuccessStatusCode();

        // Kunde 1 aus den Demo-Daten lesen (nur lesen, der Test ändert nichts).
        // JsonElement statt Customer: So bekommen wir das JSON so, wie es verschickt wurde. Würden wir es in ein
        // C#-DateTime umwandeln, wäre nicht mehr zu sehen, ob im Text ein "Z" stand oder nicht
        var customer = await client.GetFromJsonAsync<JsonElement>("/api/customers/1");

        // Das Feld createdAt als Text holen und prüfen, dass es mit "Z" endet: bei Kunde 1 "2023-01-29T10:15:00Z"
        Assert.EndsWith("Z", customer.GetProperty("createdAt").GetString());
    }
}
