using System.Text.RegularExpressions;
using PrakApp.Api.Common;

namespace PrakApp.Api.Customers;

public sealed partial class CustomerValidator
{
    public const int FirstNameMaxLength = 50;
    public const int LastNameMaxLength = 100;

    public ValidationErrors Validate(CustomerInput input)
    {
        var errors = new ValidationErrors();

        if (string.IsNullOrWhiteSpace(input.FirstName))
            errors.Add(nameof(input.FirstName), "Vorname ist erforderlich.");
        else if (input.FirstName.Trim().Length > FirstNameMaxLength)
            errors.Add(nameof(input.FirstName), $"Vorname darf höchstens {FirstNameMaxLength} Zeichen lang sein.");

        if (string.IsNullOrWhiteSpace(input.LastName))
            errors.Add(nameof(input.LastName), "Nachname ist erforderlich.");
        else if (input.LastName.Trim().Length > LastNameMaxLength)
            errors.Add(nameof(input.LastName), $"Nachname darf höchstens {LastNameMaxLength} Zeichen lang sein.");

        if (!string.IsNullOrWhiteSpace(input.Email) && !EmailPattern().IsMatch(input.Email.Trim()))
            errors.Add(nameof(input.Email), "E-Mail-Adresse ist ungültig.");

        if (!string.IsNullOrWhiteSpace(input.PostalCode) && !PostalCodePattern().IsMatch(input.PostalCode.Trim()))
            errors.Add(nameof(input.PostalCode), "Postleitzahl muss aus 5 Ziffern bestehen.");

        return errors;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^\d{5}$")]
    private static partial Regex PostalCodePattern();
}
