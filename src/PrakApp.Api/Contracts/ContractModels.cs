namespace PrakApp.Api.Contracts;

public static class ContractStatus
{
    public const string Active = "Active";
    public const string Cancelled = "Cancelled";
    public const string Ended = "Ended";
}

public sealed record ContractType(int Id, string Code, string Name, int MinTermMonths, int NoticePeriodMonths);

public sealed record ContractListItem(
    int Id,
    string ContractNumber,
    string TypeName,
    DateTime StartDate,
    DateTime? EndDate,
    decimal MonthlyFee,
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

/// <summary>Kontostand eines Vertrags. Positiver Saldo = Rückstand, negativer Saldo = Guthaben.</summary>
public sealed record BalanceInfo(int MonthsDue, decimal ExpectedTotal, decimal PaidTotal, decimal Balance);

public sealed record ContractDetail(ContractRecord Contract, BalanceInfo Balance);

public sealed record ContractInput(
    int CustomerId,
    int ContractTypeId,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal MonthlyFee);

public sealed record CancelContractRequest(string? Reason);
