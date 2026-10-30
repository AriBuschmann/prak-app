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

    // Bug: Die Suche fand keinen Ort, obwohl docs/architektur.md das verspricht. In der SQL-Abfrage im
    // CustomerRepository fehlte die Bedingung für City. Wie bei den Namen wird nach dem Anfang gesucht.
    // Würzburg passt gut: In den Demo-Daten gibt es dort Kunden, aber kein Name und keine Kundennummer beginnt
    // mit "Würz". Ein Treffer kann also nur über den Ort kommen. Der Test liest nur, er ändert nichts.
    // Der Test läuft zweimal: mit dem ganzen Ort und nur mit dem Anfang ("Würz" muss "Würzburg" finden)
    [Theory]
    [InlineData("Würzburg")]
    [InlineData("Würz")]
    public async Task Search_finds_customers_by_city(string query)
    {
        // Anmelden wie im Test oben, sonst antwortet die API mit 401
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new { userName = "praktikum" })).EnsureSuccessStatusCode();

        // Suche über die API, wie das Suchfeld sie schickt: GET /api/customers?q=...
        // Uri.EscapeDataString macht den Suchtext für die Adresse sicher, z. B. wird "ü" zu "%C3%BC".
        // Die Antwort ist eine Liste von Kunden, hier als JsonElement[] (ein JSON-Objekt pro Kunde)
        var customers = await client.GetFromJsonAsync<JsonElement[]>($"/api/customers?q={Uri.EscapeDataString(query)}");

        // Es muss Treffer geben (vor der Korrektur war die Liste leer).
        // Das ! sagt dem Compiler: Die Antwort ist hier nicht null
        Assert.NotEmpty(customers!);
        // Assert.All prüft jeden Kunden in der Liste: Alle Treffer müssen aus Würzburg sein, keine anderen
        Assert.All(customers!, c => Assert.Equal("Würzburg", c.GetProperty("city").GetString()));
    }

    // Bug: Die Suche setzt den Text in SQL "LIKE" ein. Dort sind %, _ und [ ] Platzhalter (% = beliebig viele Zeichen,
    // _ = genau ein Zeichen, [a-z] = ein Zeichen von a bis z). Wer "%" eingab, fand deshalb alle Kunden.
    // Jetzt maskiert SqlLike.Escape (src/PrakApp.Api/Data/SqlLike.cs) diese Zeichen, sie werden als normale Zeichen gesucht.
    // Kein Kunde in den Demo-Daten enthält eines dieser Zeichen (in der Datenbank geprüft), also muss die Suche
    // jeweils 0 Treffer liefern. Der Test liest nur, er ändert nichts.
    [Theory]
    // vorher: alle Kunden, weil % für "beliebig viele Zeichen" stand
    [InlineData("%")]
    // vorher: alle Kunden, weil _ für "genau ein Zeichen" stand
    [InlineData("_")]
    // vorher: alle Kunden, weil [a-z] für "ein Buchstabe von a bis z" stand
    [InlineData("[a-z]")]
    // vorher: alle "Albers", weil _ das "e" ersetzt hat. Zeigt, dass der Platzhalter auch mitten im Text wirkte
    [InlineData("Alb_rs")]
    public async Task Search_treats_like_wildcards_as_normal_characters(string query)
    {
        // Anmelden wie in den Tests oben, sonst antwortet die API mit 401
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new { userName = "praktikum" })).EnsureSuccessStatusCode();

        // Suche über die API. Uri.EscapeDataString ist hier besonders wichtig: Ein "%" in einer Adresse hat selbst
        // eine Sonderbedeutung (z. B. %C3%BC für "ü"), darum wird es zu "%25" umgeschrieben und kommt als "%" beim Server an
        var customers = await client.GetFromJsonAsync<JsonElement[]>($"/api/customers?q={Uri.EscapeDataString(query)}");

        // Keine Treffer: Das Zeichen wurde als normales Zeichen gesucht und kommt bei keinem Kunden vor
        Assert.Empty(customers!);
    }

    [Theory]
    [InlineData("wolfgang", "THIEL", "97070", true)]
    [InlineData("Wolfgang", "Thiel", "24103", false)]
    [InlineData("Wolfgang", "Thiel", "", true)]
    public async Task Duplicates_are_found_by_name_and_postal_code(string firstName, string lastName, string postalCode, bool expected)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new { userName = "praktikum" })).EnsureSuccessStatusCode();

        var url = $"/api/customers/duplicates?firstName={Uri.EscapeDataString(firstName)}&lastName={Uri.EscapeDataString(lastName)}"
            + $"&postalCode={Uri.EscapeDataString(postalCode)}";
        var duplicates = await client.GetFromJsonAsync<JsonElement[]>(url);

        var numbers = duplicates!.Select(c => c.GetProperty("customerNumber").GetString()).ToList();
        Assert.Equal(expected, numbers.Contains("K-100003"));
    }
}
