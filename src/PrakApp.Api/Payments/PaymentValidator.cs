using PrakApp.Api.Common;

namespace PrakApp.Api.Payments;

public sealed class PaymentValidator
{
    public const decimal MaxAmount = 99_999.99m;
    public const int ReferenceMaxLength = 100;

    public ValidationErrors Validate(PaymentInput input)
    {
        var errors = new ValidationErrors();

        if (input.Amount <= 0)
            errors.Add(nameof(input.Amount), "Betrag muss größer als 0 sein.");
        else if (input.Amount > MaxAmount)
            errors.Add(nameof(input.Amount), $"Betrag darf höchstens {MaxAmount:0.00} sein.");

        if (input.PaymentDate is null)
            errors.Add(nameof(input.PaymentDate), "Zahlungsdatum ist erforderlich.");

        if (input.Reference is { Length: > ReferenceMaxLength })
            errors.Add(nameof(input.Reference), $"Verwendungszweck darf höchstens {ReferenceMaxLength} Zeichen lang sein.");

        return errors;
    }
}
