using PrakApp.Api.Common;

namespace PrakApp.Api.Contracts;

public sealed class ContractValidator
{
    public const decimal MaxMonthlyFee = 9999.99m;
    // Erlaubter Zeitraum für Vertragsbeginn und -ende. Ein zweistellig getipptes Jahr ("26" → 0026) liegt immer davor
    // und führte vorher zu Fehler 500, weil Dapper ein DateTime als SQL-Typ datetime schickt (erst ab 1753 möglich).
    public static readonly DateTime MinDate = new(2000, 1, 1);
    public static readonly DateTime MaxDate = new(2099, 12, 31);

    public ValidationErrors Validate(ContractInput input)
    {
        var errors = new ValidationErrors();

        if (input.CustomerId <= 0)
            errors.Add(nameof(input.CustomerId), "Kunde ist erforderlich.");

        if (input.ContractTypeId <= 0)
            errors.Add(nameof(input.ContractTypeId), "Vertragsart ist erforderlich.");

        if (input.StartDate is null)
            errors.Add(nameof(input.StartDate), "Vertragsbeginn ist erforderlich.");
        else if (!IsInRange(input.StartDate.Value))
            errors.Add(nameof(input.StartDate), $"Vertragsbeginn muss zwischen {MinDate:dd.MM.yyyy} und {MaxDate:dd.MM.yyyy} liegen.");

        if (input.EndDate is { } end)
        {
            if (!IsInRange(end))
                errors.Add(nameof(input.EndDate), $"Vertragsende muss zwischen {MinDate:dd.MM.yyyy} und {MaxDate:dd.MM.yyyy} liegen.");
            else if (end < input.StartDate)
                errors.Add(nameof(input.EndDate), "Vertragsende darf nicht vor dem Vertragsbeginn liegen.");
        }

        if (input.MonthlyFee <= 0)
            errors.Add(nameof(input.MonthlyFee), "Monatsbeitrag muss größer als 0 sein.");
        else if (input.MonthlyFee > MaxMonthlyFee)
            errors.Add(nameof(input.MonthlyFee), $"Monatsbeitrag darf höchstens {MaxMonthlyFee:0.00} sein.");

        return errors;
    }

    private static bool IsInRange(DateTime date) => date.Date >= MinDate && date.Date <= MaxDate;
}
