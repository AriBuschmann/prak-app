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

    // Fehlender Test: Höchstlänge Vorname 50 Zeichen, passend zur Datenbank-Spalte FirstName NVARCHAR(50) (Patch 1.0.0/0002).
    // Grenzwert-Test: genau 50 Zeichen sind gerade noch erlaubt
    [Fact]
    public void First_name_may_have_50_characters()
    {
        var errors = _validator.Validate(ValidCustomer() with { FirstName = new string('A', 50) });

        Assert.True(errors.IsValid);
    }

    // Grenzwert-Test: ein Zeichen mehr (51) muss abgelehnt werden, mit genau dieser Meldung beim Feld FirstName
    [Fact]
    public void First_name_longer_than_50_characters_is_rejected()
    {
        var errors = _validator.Validate(ValidCustomer() with { FirstName = new string('A', 51) });

        Assert.Equal(["Vorname darf höchstens 50 Zeichen lang sein."], errors.ToDictionary()["FirstName"]);
    }

    // Höchstlängen von E-Mail, Telefon, Straße und Ort = Spaltenlängen in dbo.Customer.
    // Vorher prüfte der Validator diese Längen nicht: zu lange Werte führten in der Datenbank zu Fehler 500.
    // Die Zahlen stehen hier absichtlich als feste Werte und nicht als CustomerValidator.PhoneMaxLength usw.:
    // So fällt der Test auf, wenn jemand die Konstanten im Validator ändert, ohne an die Datenbank zu denken.

    // Grenzwert-Test: genau die Höchstlänge ist gerade noch erlaubt.
    // [Theory] mit [InlineData] heißt: derselbe Test läuft viermal, einmal pro Zeile mit diesen Werten
    // für die Parameter field und maxLength (z. B. field = "Phone", maxLength = 30)
    [Theory]
    [InlineData("Email", 200)]
    [InlineData("Phone", 30)]
    [InlineData("Street", 100)]
    [InlineData("City", 80)]
    public void Optional_fields_may_have_their_maximum_length(string field, int maxLength)
    {
        // Gültiger Kunde, bei dem nur das Feld "field" genau maxLength Zeichen lang ist (Hilfsmethode unten)
        var errors = _validator.Validate(WithLength(field, maxLength));

        // Keine Fehler: Der Kunde darf so gespeichert werden
        Assert.True(errors.IsValid);
    }

    // Grenzwert-Test: ein Zeichen mehr als die Höchstlänge muss abgelehnt werden (gleiche vier Fälle wie oben)
    [Theory]
    [InlineData("Email", 200)]
    [InlineData("Phone", 30)]
    [InlineData("Street", 100)]
    [InlineData("City", 80)]
    public void Optional_fields_longer_than_maximum_are_rejected(string field, int maxLength)
    {
        // Wie oben, aber mit maxLength + 1 Zeichen (z. B. 31 Zeichen beim Telefon)
        var errors = _validator.Validate(WithLength(field, maxLength + 1));

        // Der Fehler muss genau beim betroffenen Feld stehen, damit das Frontend die Meldung an diesem Eingabefeld anzeigt
        Assert.Contains(field, errors.ToDictionary().Keys);
    }

    // Hilfsmethode: liefert einen gültigen Kunden, bei dem nur das genannte Feld "length" Zeichen lang ist.
    // "field switch { ... }" wählt je nach Feldname die passende Zeile aus.
    private static CustomerInput WithLength(string field, int length) => field switch
    {
        // Die E-Mail muss trotz Länge gültig aussehen, sonst käme die Meldung "ungültig" statt der Längen-Meldung.
        // Darum: so viele 'a' wie nötig, dann "@example.org" (12 Zeichen), zusammen genau "length" Zeichen
        "Email" => ValidCustomer() with { Email = new string('a', length - "@example.org".Length) + "@example.org" },
        // Bei den anderen Feldern reicht ein Zeichen, "length"-mal wiederholt
        "Phone" => ValidCustomer() with { Phone = new string('1', length) },
        "Street" => ValidCustomer() with { Street = new string('S', length) },
        "City" => ValidCustomer() with { City = new string('O', length) },
        // _ steht für "alle anderen Werte": Ein Tippfehler im Feldnamen bei [InlineData] fällt so sofort auf
        _ => throw new ArgumentException($"Unbekanntes Feld: {field}"),
    };

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
