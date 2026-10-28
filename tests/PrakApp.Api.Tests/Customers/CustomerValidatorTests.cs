using PrakApp.Api.Customers;

namespace PrakApp.Api.Tests.Customers;

public class CustomerValidatorTests
{
    private readonly CustomerValidator _validator = new();

    private static CustomerInput ValidCustomer() =>
        new("Anna", "Albers", "anna.albers@example.org", "0431 123456", "Lindenweg 3", "24103", "Kiel");

    [Fact]
    public void Valid_customer_has_no_errors()
    {
        var errors = _validator.Validate(ValidCustomer());

        Assert.True(errors.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void First_name_is_required(string? firstName)
    {
        var errors = _validator.Validate(ValidCustomer() with { FirstName = firstName });

        Assert.False(errors.IsValid);
        Assert.Contains("FirstName", errors.ToDictionary().Keys);
    }

    [Fact]
    public void Last_name_is_required()
    {
        var errors = _validator.Validate(ValidCustomer() with { LastName = "" });

        Assert.Contains("LastName", errors.ToDictionary().Keys);
    }

    // Höchstlänge 100 Zeichen, passend zur Datenbank-Spalte LastName NVARCHAR(100) (Patch 1.1.0/0002)
    // Grenzwert-Test: genau 100 Zeichen sind gerade noch erlaubt
    [Fact]
    public void Last_name_may_have_100_characters()
    {
        // Gültiger Kunde, nur der Nachname wird ersetzt: "with" macht eine Kopie des Records mit geändertem Feld,
        // new string('A', 100) ergibt einen Text aus 100-mal dem Buchstaben A
        var errors = _validator.Validate(ValidCustomer() with { LastName = new string('A', 100) });

        // Keine Fehler: Der Kunde darf gespeichert werden
        Assert.True(errors.IsValid);
    }

    // Grenzwert-Test: ein Zeichen mehr (101) muss abgelehnt werden
    [Fact]
    public void Last_name_longer_than_100_characters_is_rejected()
    {
        // Wie oben, aber mit 101 Zeichen
        var errors = _validator.Validate(ValidCustomer() with { LastName = new string('A', 101) });

        // Der Fehler muss beim Feld "LastName" stehen, damit das Frontend die Meldung am Eingabefeld anzeigt
        Assert.Contains("LastName", errors.ToDictionary().Keys);
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("123456")]
    [InlineData("24l03")]
    public void Postal_code_must_have_five_digits(string postalCode)
    {
        var errors = _validator.Validate(ValidCustomer() with { PostalCode = postalCode });

        Assert.Contains("PostalCode", errors.ToDictionary().Keys);
    }

    [Theory]
    [InlineData("anna")]
    [InlineData("anna@")]
    [InlineData("anna@example")]
    public void Email_must_look_like_an_address(string email)
    {
        var errors = _validator.Validate(ValidCustomer() with { Email = email });

        Assert.Contains("Email", errors.ToDictionary().Keys);
    }

    [Fact]
    public void Optional_fields_may_be_empty()
    {
        var errors = _validator.Validate(new CustomerInput("Anna", "Albers", null, null, null, null, null));

        Assert.True(errors.IsValid);
    }
}
