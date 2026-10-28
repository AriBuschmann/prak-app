using Microsoft.AspNetCore.Mvc;
using PrakApp.Api.Common;

namespace PrakApp.Api.Contracts;

[ApiController]
[Route("api/contracts")]
public sealed class ContractsController(ContractService service, IContractRepository contracts) : ControllerBase
{
    [HttpGet("/api/contract-types")]
    public Task<IReadOnlyList<ContractType>> GetTypes() => contracts.GetTypesAsync();

    // Suche nach Vertragsnummer, z. B. aus dem Verwendungszweck einer Überweisung. Ohne Suchtext keine Treffer,
    // damit nicht versehentlich alle Verträge geladen werden
    [HttpGet]
    public async Task<IReadOnlyList<ContractSearchItem>> Search([FromQuery] string? q) =>
        string.IsNullOrWhiteSpace(q) ? [] : await contracts.SearchByNumberAsync(q);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContractDetail>> Get(int id) =>
        await service.GetDetailAsync(id) is { } detail ? detail : NotFound();

    [HttpPost]
    public async Task<ActionResult<ContractDetail>> Create(ContractInput input)
    {
        var (id, errors) = await service.CreateAsync(input, User.UserName());
        if (id is null)
        {
            return ValidationProblem(new ValidationProblemDetails(errors.ToDictionary()));
        }

        return CreatedAtAction(nameof(Get), new { id }, await service.GetDetailAsync(id.Value));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancelContractRequest request)
    {
        // Geändert (Schritt 2): Der Service gibt jetzt Ergebnis und Fehler zusammen zurück (ein Tupel),
        // "var (result, errors)" zerlegt es in zwei Variablen. Vorher wurde nur der Grund übergeben.
        var (result, errors) = await service.CancelAsync(id, request);
        return result switch
        {
            // Neu (Schritt 2): ungültiger Kündigungsgrund → 400 mit den Fehlern pro Feld, wie überall im Projekt
            CancelResult.Invalid => ValidationProblem(new ValidationProblemDetails(errors.ToDictionary())),
            CancelResult.NotFound => NotFound(),
            CancelResult.NotActive => Conflict(new ProblemDetails { Title = "Nur aktive Verträge können gekündigt werden." }),
            _ => NoContent(),
        };
    }

    // Neu (Schritt 4): voraussichtliches Vertragsende für den Kündigungsdialog, ohne etwas zu speichern
    [HttpGet("{id:int}/cancellation-preview")]
    public async Task<ActionResult<CancellationPreview>> GetCancellationPreview(int id)
    {
        var (result, preview) = await service.PreviewCancellationAsync(id);
        return result switch
        {
            CancelResult.NotFound => NotFound(),
            CancelResult.NotActive => Conflict(new ProblemDetails { Title = "Nur aktive Verträge können gekündigt werden." }),
            _ => preview!,
        };
    }
}
