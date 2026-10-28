namespace PrakApp.Api.Contracts;

public static class ContractStatus
{
    public const string Active = "Active";
    public const string Cancelled = "Cancelled";
    public const string Ended = "Ended";

    /// <summary>
    /// Status, mit dem die App arbeitet: Ist das Vertragsende vorbei, ist der Vertrag "Ended", egal was gespeichert ist.
    /// Das Vertragsende ist der letzte Tag des Vertrags, beendet ist er also erst ab dem Tag danach.
    /// Wird beim Lesen berechnet (ContractRepository), damit es ohne zeitgesteuerten Job immer stimmt.
    /// </summary>
    public static string Effective(string storedStatus, DateTime? endDate, DateTime today) =>
        endDate is { } end && end.Date < today.Date ? Ended : storedStatus;
}

public sealed record ContractType(int Id, string Code, string Name, int MinTermMonths, int NoticePeriodMonths);

public sealed record ContractListItem(
    int Id,
    string ContractNumber,
    string TypeName,
    DateTime StartDate,
    DateTime? EndDate,
    decimal MonthlyFee,
    string Status,
    decimal PaidTotal)
{
    // Kommt nicht aus der Datenbank, sondern wird im ContractRepository berechnet (darum nicht in der Klammer oben)
    public bool ReminderDue { get; init; }
}

/// <summary>Treffer der Suche nach Vertragsnummer (GET /api/contracts?q=...).</summary>
public sealed record ContractSearchItem(
    int Id,
    string ContractNumber,
    string CustomerName,
    DateTime? EndDate,
    string Status);

public sealed record ContractRecord(
    int Id,
    string ContractNumber,
    int CustomerId,
    string CustomerName,
    int ContractTypeId,
    string TypeName,
    int MinTermMonths,
    int NoticePeriodMonths,
    DateTime StartDate,
    DateTime? EndDate,
    decimal MonthlyFee,
    string Status,
    DateTime? CancelledAt,
    string? CancellationReason);

/// <summary>
/// Kontostand eines Vertrags. Positiver Saldo = Rückstand, negativer Saldo = Guthaben.
/// ReminderDue = Zahlungserinnerung fällig (Rückstand über zwei Monatsbeiträge, siehe BalanceCalculator.IsReminderDue).
/// </summary>
public sealed record BalanceInfo(int MonthsDue, decimal ExpectedTotal, decimal PaidTotal, decimal Balance, bool ReminderDue);

public sealed record ContractDetail(ContractRecord Contract, BalanceInfo Balance);

public sealed record ContractInput(
    int CustomerId,
    int ContractTypeId,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal MonthlyFee);

public sealed record CancelContractRequest(string? Reason);

/// <summary>
/// Neu (Schritt 4): Vorschau für den Kündigungsdialog. Zeigt, wann der Vertrag enden würde,
/// wenn man heute kündigt.
/// </summary>

public sealed record CancellationPreview(DateTime CancelledAt, DateTime EndDate);
