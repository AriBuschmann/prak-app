using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PrakApp.Api.Tests.Contracts;

// Verbesserung: Suche nach Vertragsnummer (GET /api/contracts?q=...), damit man eine Zahlung mit Vertragsnummer
// im Verwendungszweck direkt dem richtigen Vertrag zuordnen kann. Vorher gab es diese Suche nicht (404).
// Wie die anderen Endpunkt-Tests startet WebApplicationFactory<Program> die API im Speicher, mit der normalen Datenbank.
// Die Tests lesen nur, sie ändern nichts.
public class ContractSearchEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    // Hilfsmethode für alle Tests: anmelden, suchen und die Treffer zurückgeben
    private async Task<JsonElement[]> SearchAsync(string query)
    {
        // Client wie ein Browser, der sich das Anmelde-Cookie merkt; ohne Anmeldung käme 401
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new { userName = "praktikum" })).EnsureSuccessStatusCode();
        // Suchtext für die Adresse sicher machen (z. B. "%" → "%25") und die Antwort als Liste von JSON-Objekten lesen.
        // Das ! sagt dem Compiler, dass die Antwort hier nicht null ist
        return (await client.GetFromJsonAsync<JsonElement[]>($"/api/contracts?q={Uri.EscapeDataString(query)}"))!;
    }

    // Vertrag 77 aus den Demo-Daten (V-2023-0077, Kundin Clara Quast) wird über seine Nummer gefunden.
    // Zweiter Fall in Kleinbuchstaben: Groß-/Kleinschreibung spielt keine Rolle (wer die Nummer abtippt, achtet nicht darauf)
    [Theory]
    [InlineData("V-2023-0077")]
    [InlineData("v-2023-0077")]
    public async Task Search_finds_contract_by_its_number(string query)
    {
        var contracts = await SearchAsync(query);

        // Assert.Single prüft: genau ein Treffer, und gibt ihn zurück, damit wir ihn weiter prüfen können
        var contract = Assert.Single(contracts);
        // Die Id braucht das Frontend für den Link zur Vertragsseite (/contracts/77)
        Assert.Equal(77, contract.GetProperty("id").GetInt32());
        Assert.Equal("V-2023-0077", contract.GetProperty("contractNumber").GetString());
        // Der Kundenname wird im Treffer mitgeliefert, damit man sieht, wem der Vertrag gehört
        Assert.Equal("Clara Quast", contract.GetProperty("customerName").GetString());
    }

    // Ohne echten Suchtext darf nichts kommen:
    // "" (leer) soll nicht alle Verträge liefern, sonst würde jede leere Suche die ganze Liste laden.
    // "%" ist bei SQL LIKE ein Platzhalter und würde ohne Maskierung (SqlLike.Escape) alle Verträge finden
    [Theory]
    [InlineData("")]
    [InlineData("%")]
    public async Task Search_without_real_text_finds_nothing(string query)
    {
        Assert.Empty(await SearchAsync(query));
    }
}
