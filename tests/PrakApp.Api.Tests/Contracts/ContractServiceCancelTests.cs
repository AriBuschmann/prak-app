using PrakApp.Api.Contracts;

namespace PrakApp.Api.Tests.Contracts;

/// <summary>
/// Prüft ContractService.CancelAsync ohne Datenbank: Statt des echten Repositorys wird ein Fake benutzt,
/// das nur festhält, was gespeichert werden soll. "Heute" ist fest der 13.04.2026.
/// </summary>
public class ContractServiceCancelTests
{
    // Festes "Heute", damit die Tests an jedem Tag dasselbe Ergebnis liefern
    private static readonly DateTime Today = new(2026, 4, 13);

    // KOMFORT-Vertrag: Beginn 01.03.2026, 12 Monate Mindestlaufzeit, 3 Monate Kündigungsfrist
    // Status und Vertragsende sind optional, so kann jeder Test nur das ändern, was er braucht
    private static ContractRecord Komfort(string status = ContractStatus.Active, DateTime? endDate = null) =>
        new(7, "V-2026-0007", 1, "Anna Albers", 2, "Komfort-Tarif", MinTermMonths: 12, NoticePeriodMonths: 3,
            StartDate: new DateTime(2026, 3, 1), EndDate: endDate, MonthlyFee: 24.90m, Status: status,
            CancelledAt: null, CancellationReason: null);

    // Kunden- und Zahlungs-Repository braucht CancelAsync nicht, deshalb null! (wird nie benutzt)
    // Im Test gibt es keinen DI-Container: Wir erzeugen den Service selbst und geben ihm das Fake statt des echten Repositorys
    private static ContractService Service(FakeContractRepository contracts) =>
        new(contracts, customers: null!, payments: null!, new ContractValidator(), new CancellationValidator(),
            new BalanceCalculator(new FixedTimeProvider(Today)), new FixedTimeProvider(Today));

    [Fact]
    public async Task Cancelling_saves_status_date_reason_and_calculated_end_date()
    {
        var repository = new FakeContractRepository(Komfort());

        // Grund mit Leerzeichen am Rand, um auch das Kürzen zu prüfen. "_" heißt: die Fehlerliste brauchen wir hier nicht
        var (result, _) = await Service(repository).CancelAsync(7, new CancelContractRequest("  Umzug  "));

        Assert.Equal(CancelResult.Cancelled, result);
        // Das Fake muss etwas gespeichert haben; .Value holt danach die Werte aus dem (sonst leer möglichen) Tupel
        Assert.NotNull(repository.Saved);
        // Regel 3: Kündigungsdatum = heute
        Assert.Equal(Today, repository.Saved.Value.CancelledAt);
        // Regel 4 ergäbe 31.07.2026, die Mindestlaufzeit (Regel 5) geht aber bis 28.02.2027
        Assert.Equal(new DateTime(2027, 2, 28), repository.Saved.Value.EndDate);
        Assert.Equal("Umzug", repository.Saved.Value.Reason);
    }

    // Regel 8: Das frühere vorhandene Vertragsende eines befristeten Vertrags bleibt
    [Fact]
    public async Task Existing_earlier_end_date_is_kept()
    {
        var repository = new FakeContractRepository(Komfort(endDate: new DateTime(2026, 12, 31)));

        await Service(repository).CancelAsync(7, new CancelContractRequest("Umzug"));

        // "?." liest EndDate nur, wenn etwas gespeichert wurde; sonst ist das Ergebnis null und der Test schlägt fehl
        Assert.Equal(new DateTime(2026, 12, 31), repository.Saved?.EndDate);
    }

    // Regel 1: Nur aktive Verträge können gekündigt werden
    [Fact]
    public async Task Contract_that_is_not_active_is_not_cancelled()
    {
        var repository = new FakeContractRepository(Komfort(status: ContractStatus.Cancelled));

        var (result, _) = await Service(repository).CancelAsync(7, new CancelContractRequest("Umzug"));

        Assert.Equal(CancelResult.NotActive, result);
        // Null = das Fake hat nichts gespeichert, der Vertrag wurde also nicht noch einmal gekündigt
        Assert.Null(repository.Saved);
    }

    [Fact]
    public async Task Unknown_contract_is_not_found()
    {
        var repository = new FakeContractRepository(Komfort());

        // Das Fake kennt nur Vertrag 7; für 999 liefert GetAsync null, wie die echte Datenbank bei einer unbekannten Id
        var (result, _) = await Service(repository).CancelAsync(999, new CancelContractRequest("Umzug"));

        Assert.Equal(CancelResult.NotFound, result);
        Assert.Null(repository.Saved);
    }

    // Regel 2: Ohne Grund wird nichts gespeichert, und der Fehler steht beim Feld "Reason"
    [Fact]
    public async Task Invalid_reason_saves_nothing()
    {
        var repository = new FakeContractRepository(Komfort());

        var (result, errors) = await Service(repository).CancelAsync(7, new CancelContractRequest(""));

        Assert.Equal(CancelResult.Invalid, result);
        Assert.Contains("Reason", errors.ToDictionary().Keys);
        Assert.Null(repository.Saved);
    }

    // Neu (Schritt 4): Vorschau für den Kündigungsdialog,  zeigt das voraussichtliche Ende, speichert aber nichts
    [Fact]
    public async Task Preview_shows_the_expected_end_date_without_saving()
    {
        var repository = new FakeContractRepository(Komfort());

        var (result, preview) = await Service(repository).PreviewCancellationAsync(7);

        Assert.Equal(CancelResult.Cancelled, result);
        Assert.Equal(Today, preview?.CancelledAt);
        // Dieselbe Berechnung wie beim echten Kündigen (siehe erster Test)
        Assert.Equal(new DateTime(2027, 2, 28), preview?.EndDate);
        Assert.Null(repository.Saved);
    }

    [Fact]
    public async Task Preview_is_not_possible_for_a_contract_that_is_not_active()
    {
        var repository = new FakeContractRepository(Komfort(status: ContractStatus.Ended));

        var (result, preview) = await Service(repository).PreviewCancellationAsync(7);

        Assert.Equal(CancelResult.NotActive, result);
        Assert.Null(preview);
    }

    /// <summary>Ersatz für das echte Repository: kennt genau einen Vertrag und merkt sich, was gespeichert wird.</summary>
    // Es implementiert dasselbe Interface IContractRepository wie das echte, deshalb nimmt der Service es ohne Änderung an
    private sealed class FakeContractRepository(ContractRecord contract) : IContractRepository
    {
        // Was CancelAsync speichern wollte, als Tupel, null heißt: es wurde (noch) nichts gespeichert
        public (int Id, DateTime CancelledAt, DateTime EndDate, string Reason)? Saved { get; private set; }

        // Liefert den Vertrag nur bei passender Id. Task.FromResult verpackt den Wert, weil die Methode async-fähig sein muss
        public Task<ContractRecord?> GetAsync(int id) => Task.FromResult(id == contract.Id ? contract : null);

        // Statt SQL auszuführen, merkt sich das Fake nur die Werte, damit der Test sie prüfen kann
        public Task CancelAsync(int id, DateTime cancelledAt, DateTime endDate, string reason)
        {
            Saved = (id, cancelledAt, endDate, reason);
            return Task.CompletedTask;
        }

        // Diese Methoden braucht CancelAsync nicht
        // Würde der Service sie doch aufrufen, bricht der Test mit NotSupportedException ab und wir merken es sofort
        public Task<IReadOnlyList<ContractType>> GetTypesAsync() => throw new NotSupportedException();
        public Task<IReadOnlyList<ContractListItem>> GetByCustomerAsync(int customerId) => throw new NotSupportedException();
        public Task<IReadOnlyList<ContractSearchItem>> SearchByNumberAsync(string query) => throw new NotSupportedException();
        public Task<int> CreateAsync(ContractInput input, string createdBy) => throw new NotSupportedException();
    }

    // Ersatz für die Systemuhr: "Jetzt" ist immer 12:00 Uhr am angegebenen Tag. Mit Zeitzone UTC und Mittag
    // ergibt GetLocalNow().Date auf jedem Rechner denselben Tag, egal wie dessen Zeitzone eingestellt ist
    private sealed class FixedTimeProvider(DateTime today) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(today.AddHours(12), TimeSpan.Zero);

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
