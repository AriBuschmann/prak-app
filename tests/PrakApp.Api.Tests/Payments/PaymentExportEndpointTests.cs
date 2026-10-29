using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PrakApp.Api.Tests.Payments;

// Verbesserung: GET /api/contracts/{id}/payments/export liefert die Zahlungen als CSV-Datei.
// Wie die anderen Endpunkt-Tests mit der normalen Datenbank; die Tests lesen nur, sie ändern nichts
public class PaymentExportEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private async Task<HttpClient> LoginAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new { userName = "praktikum" })).EnsureSuccessStatusCode();
        return client;
    }

    [Fact]
    public async Task Export_returns_csv_file_with_the_payments_of_the_contract()
    {
        var client = await LoginAsync();

        var response = await client.GetAsync("/api/contracts/77/payments/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("zahlungen-V-2023-0077.csv", response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        var lines = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3).Split("\r\n");
        Assert.Equal("Vertragsnummer;Zahlungsdatum;Betrag;Verwendungszweck;Erfasst von", lines[0]);
        // Zahlung 1556 aus den Demo-Daten (weitere Zahlungen können von Playwright-Läufen dazugekommen sein)
        Assert.Contains("V-2023-0077;06.09.2026;24,90;Beitrag 09/2026;s.okafor", lines);
    }

    [Fact]
    public async Task Export_of_unknown_contract_is_not_found()
    {
        var client = await LoginAsync();

        var response = await client.GetAsync("/api/contracts/999999/payments/export");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
