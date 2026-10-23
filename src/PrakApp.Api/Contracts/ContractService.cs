using PrakApp.Api.Common;
using PrakApp.Api.Customers;
using PrakApp.Api.Payments;

namespace PrakApp.Api.Contracts;

public enum CancelResult
{
    Cancelled,
    NotFound,
    NotActive,
    Invalid, // Neu (Schritt 2): Kündigungsgrund fehlt oder ist zu lang → Controller antwortet mit 400
}

public sealed class ContractService(
    IContractRepository contracts,
    ICustomerRepository customers,
    IPaymentRepository payments,
    ContractValidator validator,
    // Neu (Schritt 2): Validator für den Kündigungsgrund, kommt per Dependency Injection (registriert in Program.cs)
    CancellationValidator cancellationValidator,
    BalanceCalculator balanceCalculator,
    TimeProvider time)
{
    public async Task<ContractDetail?> GetDetailAsync(int id)
    {
        var contract = await contracts.GetAsync(id);
        if (contract is null)
        {
            return null;
        }

        var paid = await payments.GetTotalAsync(id);
        return new ContractDetail(contract, balanceCalculator.Calculate(contract, paid));
    }

    public async Task<(int? Id, ValidationErrors Errors)> CreateAsync(ContractInput input, string createdBy)
    {
        var errors = validator.Validate(input);
        if (errors.IsValid)
        {
            if (await customers.GetAsync(input.CustomerId) is null)
                errors.Add(nameof(input.CustomerId), "Kunde existiert nicht.");

            var types = await contracts.GetTypesAsync();
            if (types.All(t => t.Id != input.ContractTypeId))
                errors.Add(nameof(input.ContractTypeId), "Vertragsart existiert nicht.");
        }

        if (!errors.IsValid)
        {
            return (null, errors);
        }

        return (await contracts.CreateAsync(input, createdBy), errors);
    }

    // Geändert (Schritt 2): Früher bekam die Methode nur den Grund (string? reason) und gab nur CancelResult zurück.
    // Jetzt bekommt sie die ganze Anfrage und gibt zusätzlich die Fehler zurück, nach demselben Muster wie CreateAsync,
    // damit der Controller bei CancelResult.Invalid die Fehlermeldungen pro Feld anzeigen kann.
    public async Task<(CancelResult Result, ValidationErrors Errors)> CancelAsync(int id, CancelContractRequest request)
    {
        // Zuerst die Eingabe prüfen (wie bei CreateAsync), dafür ist keine Datenbankabfrage nötig
        var errors = cancellationValidator.Validate(request);
        if (!errors.IsValid)
        {
            return (CancelResult.Invalid, errors);
        }

        var contract = await contracts.GetAsync(id);
        if (contract is null)
        {
            return (CancelResult.NotFound, errors);
        }
        if (contract.Status != ContractStatus.Active)
        {
            return (CancelResult.NotActive, errors);
        }

        // Geändert (Schritt 4): Kündigungsdatum und Vertragsende kommen jetzt aus Preview(), damit Kündigung und
        // Vorschau im Dialog garantiert dasselbe Datum ergeben
        var preview = Preview(contract);
        // Geändert (Schritt 2): Grund ohne Leerzeichen am Anfang und Ende speichern. Das "!" sagt dem Compiler,
        // dass Reason hier nicht null sein kann, das hat der Validator oben schon sichergestellt.
        // Geändert (Schritt 3): Das Vertragsende wird jetzt mitgespeichert.
        await contracts.CancelAsync(id, preview.CancelledAt, preview.EndDate, request.Reason!.Trim());
        return (CancelResult.Cancelled, errors);
    }

    /// <summary>
    /// Neu (Schritt 4): Berechnet, was eine Kündigung heute ergeben würde, ohne etwas zu speichern.
    /// CancelResult.Cancelled bedeutet hier: Der Vertrag kann gekündigt werden.
    /// </summary>
    public async Task<(CancelResult Result, CancellationPreview? Preview)> PreviewCancellationAsync(int id)
    {
        var contract = await contracts.GetAsync(id);
        if (contract is null)
        {
            return (CancelResult.NotFound, null);
        }
        if (contract.Status != ContractStatus.Active)
        {
            return (CancelResult.NotActive, null);
        }
        return (CancelResult.Cancelled, Preview(contract));
    }

    private CancellationPreview Preview(ContractRecord contract)
    {
        // Regel 3: Kündigungsdatum ist der Tag, an dem die Kündigung erfasst wird
        var today = time.GetLocalNow().Date;
        // Neu (Schritt 3): Vertragsende nach den Regeln 4, 5, 8 und 9 berechnen. Kündigungsfrist und Mindestlaufzeit
        // kommen aus dem Tarif (GetAsync liest sie aus ContractType), contract.EndDate ist das Ende befristeter Verträge.
        var endDate = CancellationCalculator.EndDate(
            contract.StartDate, today, contract.NoticePeriodMonths, contract.MinTermMonths, contract.EndDate);
        return new CancellationPreview(today, endDate);
    }
}
