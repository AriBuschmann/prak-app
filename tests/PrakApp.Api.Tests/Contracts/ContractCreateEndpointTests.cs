using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PrakApp.Api.Tests.Contracts;

// Fehlender Test: Vertrag anlegen (POST /api/contracts) mit einem Kunden oder einer Vertragsart, die es nicht gibt.
// Das prüft ContractService.CreateAsync mit einer Abfrage in der Datenbank, deshalb als Endpunkt-Test mit der normalen
// Datenbank. Beide Anfragen werden mit 400 abgelehnt, es wird also kein Vertrag angelegt
public class ContractCreateEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private async Task<ValidationProblemDetails> CreateAndExpect400Async(int customerId, int contractTypeId)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new { userName = "praktikum" })).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/contracts", new
        {
            customerId, contractTypeId, startDate = "2026-11-01", endDate = (string?)null, monthlyFee = 24.90m,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
    }

    [Fact]
    public async Task Unknown_customer_is_rejected()
    {
        // Kunde 999999 gibt es nicht, Vertragsart 1 (Basis-Tarif) schon
        var problem = await CreateAndExpect400Async(customerId: 999999, contractTypeId: 1);

        Assert.Equal(["Kunde existiert nicht."], problem.Errors["CustomerId"]);
        Assert.False(problem.Errors.ContainsKey("ContractTypeId"));
    }

    [Fact]
    public async Task Unknown_contract_type_is_rejected()
    {
        // Kunde 1 gibt es, Vertragsart 999 nicht (in den Demo-Daten gibt es die Vertragsarten 1 bis 4)
        var problem = await CreateAndExpect400Async(customerId: 1, contractTypeId: 999);

        Assert.Equal(["Vertragsart existiert nicht."], problem.Errors["ContractTypeId"]);
        Assert.False(problem.Errors.ContainsKey("CustomerId"));
    }
}
