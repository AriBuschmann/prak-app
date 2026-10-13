using PrakApp.Api.Common;

namespace PrakApp.Api.Contracts;

public sealed class ContractValidator
{
    public const decimal MaxMonthlyFee = 9999.99m;

    public ValidationErrors Validate(ContractInput input)
    {
        var errors = new ValidationErrors();

        if (input.CustomerId <= 0)
            errors.Add(nameof(input.CustomerId), "Kunde ist erforderlich.");

        if (input.ContractTypeId <= 0)
            errors.Add(nameof(input.ContractTypeId), "Vertragsart ist erforderlich.");

        if (input.StartDate is null)
            errors.Add(nameof(input.StartDate), "Vertragsbeginn ist erforderlich.");
        else if (input.EndDate is { } end && end < input.StartDate)
            errors.Add(nameof(input.EndDate), "Vertragsende darf nicht vor dem Vertragsbeginn liegen.");

        if (input.MonthlyFee <= 0)
            errors.Add(nameof(input.MonthlyFee), "Monatsbeitrag muss größer als 0 sein.");
        else if (input.MonthlyFee > MaxMonthlyFee)
            errors.Add(nameof(input.MonthlyFee), $"Monatsbeitrag darf höchstens {MaxMonthlyFee:0.00} sein.");

        return errors;
    }
}
