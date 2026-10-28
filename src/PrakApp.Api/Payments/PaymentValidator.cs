using PrakApp.Api.Common;

namespace PrakApp.Api.Payments;

// TimeProvider: liefert "heute" (in Program.cs als TimeProvider.System registriert, in Tests ein fester Tag)
public sealed class PaymentValidator(TimeProvider time)
{
    public const decimal MaxAmount = 99_999.99m;
    public const int ReferenceMaxLength = 100;
    // Untergrenze wie beim Vertragsbeginn (ContractValidator.MinDate). Das Datumsfeld setzt bei Backspace sonst z. B. 1900 ein
    public static readonly DateTime MinDate = new(2000, 1, 1);

    public ValidationErrors Validate(PaymentInput input)
    {
        var errors = new ValidationErrors();

        if (input.Amount <= 0)
            errors.Add(nameof(input.Amount), "Betrag muss größer als 0 sein.");
        else if (input.Amount > MaxAmount)
            errors.Add(nameof(input.Amount), $"Betrag darf höchstens {MaxAmount:0.00} sein.");

        // Zahlungsdatum: erst prüfen, ob es da ist, dann ob es im erlaubten Zeitraum liegt.
        // "else if": Pro Datum kommt höchstens eine Meldung, z. B. bei null nicht zusätzlich noch "vor 2000"
        if (input.PaymentDate is null)
            errors.Add(nameof(input.PaymentDate), "Zahlungsdatum ist erforderlich.");
        // .Value holt das Datum aus dem DateTime? (hier sicher nicht null), .Date schneidet eine eventuelle Uhrzeit ab
        else if (input.PaymentDate.Value.Date < MinDate)
            errors.Add(nameof(input.PaymentDate), $"Zahlungsdatum darf nicht vor dem {MinDate:dd.MM.yyyy} liegen.");
        // Neu: Regel aus docs/api.md, war vorher nicht umgesetzt. GetLocalNow().Date = heute (Ortszeit des Servers)
        else if (input.PaymentDate.Value.Date > time.GetLocalNow().Date)
            errors.Add(nameof(input.PaymentDate), "Zahlungsdatum darf nicht in der Zukunft liegen.");

        if (input.Reference is { Length: > ReferenceMaxLength })
            errors.Add(nameof(input.Reference), $"Verwendungszweck darf höchstens {ReferenceMaxLength} Zeichen lang sein.");

        return errors;
    }
}
