using System.Text.RegularExpressions;
using PrakApp.Api.Common;

namespace PrakApp.Api.Customers;

public sealed partial class CustomerValidator
{
    public const int FirstNameMaxLength = 50;
    public const int LastNameMaxLength = 100;
    // Gleiche Längen wie die Spalten in dbo.Customer, sonst bricht das Speichern mit Fehler 500 ab
    public const int EmailMaxLength = 200;
    public const int PhoneMaxLength = 30;
    public const int StreetMaxLength = 100;
    public const int CityMaxLength = 80;

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

        if (input.Email?.Trim().Length > EmailMaxLength)
            errors.Add(nameof(input.Email), $"E-Mail-Adresse darf höchstens {EmailMaxLength} Zeichen lang sein.");
        else if (!string.IsNullOrWhiteSpace(input.Email) && !EmailPattern().IsMatch(input.Email.Trim()))
            errors.Add(nameof(input.Email), "E-Mail-Adresse ist ungültig.");

        if (input.Phone?.Trim().Length > PhoneMaxLength)
            errors.Add(nameof(input.Phone), $"Telefon darf höchstens {PhoneMaxLength} Zeichen lang sein.");

        if (input.Street?.Trim().Length > StreetMaxLength)
            errors.Add(nameof(input.Street), $"Straße darf höchstens {StreetMaxLength} Zeichen lang sein.");

        if (input.City?.Trim().Length > CityMaxLength)
            errors.Add(nameof(input.City), $"Ort darf höchstens {CityMaxLength} Zeichen lang sein.");

        if (!string.IsNullOrWhiteSpace(input.PostalCode) && !PostalCodePattern().IsMatch(input.PostalCode.Trim()))
            errors.Add(nameof(input.PostalCode), "Postleitzahl muss aus 5 Ziffern bestehen.");

        return errors;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^\d{5}$")]
    private static partial Regex PostalCodePattern();
}
