namespace PrakApp.Api.Payments;

public sealed record Payment(
    int Id,
    int ContractId,
    decimal Amount,
    DateTime PaymentDate,
    string? Reference,
    DateTime RecordedAt,
    string RecordedBy);

public sealed record PaymentInput(decimal Amount, DateTime? PaymentDate, string? Reference);
