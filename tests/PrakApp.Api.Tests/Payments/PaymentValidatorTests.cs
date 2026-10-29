using PrakApp.Api.Payments;

namespace PrakApp.Api.Tests.Payments;

// Bug: Backspace im Zahlungsdatum setzte das Jahr 1900 ein, und der Server speicherte das ohne Prüfung.
// Jetzt prüft der PaymentValidator: Zahlungsdatum ist Pflicht, nicht vor dem 01.01.2000 und nicht in der Zukunft.
public class PaymentValidatorTests
{
    // Der Validator braucht "heute", um "nicht in der Zukunft" zu prüfen. FixedTimeProvider (unten) liefert immer
    // den 28.10.2026, 12 Uhr. So hängen die Tests nicht vom echten Datum ab und laufen auch morgen noch gleich
    private readonly PaymentValidator _validator =
        new(new FixedTimeProvider(new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero)));

    // Hilfsmethode: eine gültige Zahlung (Betrag und Verwendungszweck passen), nur das Datum ist frei wählbar.
    // DateTime? = das Datum darf auch null (leer) sein
    private static PaymentInput Payment(DateTime? paymentDate) => new(24.90m, paymentDate, "Beitrag 10/2026");

    // Erlaubt sind heute (der festgelegte 28.10.2026) und der erste erlaubte Tag 01.01.2000 (Grenzwerte)
    [Theory]
    [InlineData("2026-10-28")]
    [InlineData("2000-01-01")]
    public void Payment_date_in_the_allowed_range_is_valid(string paymentDate)
    {
        Assert.True(_validator.Validate(Payment(Date(paymentDate))).IsValid);
    }

    // Abgelehnt wird, was außerhalb liegt; zu jedem Fall steht die erwartete Meldung als zweiter Wert dabei
    [Theory]
    // Das Datum, das Backspace im Datumsfeld eingesetzt hat (vorher gespeichert, jetzt abgelehnt)
    [InlineData("1900-10-28", "Zahlungsdatum darf nicht vor dem 01.01.2000 liegen.")]
    // Grenzwert: ein Tag vor dem ersten erlaubten Tag
    [InlineData("1999-12-31", "Zahlungsdatum darf nicht vor dem 01.01.2000 liegen.")]
    // Grenzwert: morgen, also in der Zukunft (Regel stand schon in docs/api.md, war aber nicht umgesetzt)
    [InlineData("2026-10-29", "Zahlungsdatum darf nicht in der Zukunft liegen.")]
    public void Payment_date_outside_the_allowed_range_is_rejected(string paymentDate, string message)
    {
        var errors = _validator.Validate(Payment(Date(paymentDate)));

        // Genau eine Meldung beim Feld PaymentDate, und zwar genau diese
        Assert.Equal([message], errors.ToDictionary()["PaymentDate"]);
    }

    // Ein leeres Datum (null) wird weiterhin als "erforderlich" gemeldet. Das passiert jetzt, wenn man das Datum mit
    // Backspace löscht, denn das Frontend schickt dann null statt 1900
    [Fact]
    public void Payment_date_is_required()
    {
        var errors = _validator.Validate(Payment(null));

        Assert.Equal(["Zahlungsdatum ist erforderlich."], errors.ToDictionary()["PaymentDate"]);
    }

    // Fehlender Test: Der Betrag muss größer als 0 sein
    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-24.90")]
    public void Amount_of_zero_or_less_is_rejected(string amount)
    {
        var errors = _validator.Validate(Payment(Date("2026-10-28")) with { Amount = Amount(amount) });

        Assert.Equal(["Betrag muss größer als 0 sein."], errors.ToDictionary()["Amount"]);
    }

    // Fehlender Test: höchstens 99.999,99. Genau an der Grenze erlaubt, einen Cent darüber nicht.
    // Nur der Anfang der Meldung wird geprüft: Die Zahl darin hängt von der Spracheinstellung des Rechners ab
    [Fact]
    public void Amount_at_the_maximum_is_allowed()
    {
        Assert.True(_validator.Validate(Payment(Date("2026-10-28")) with { Amount = 99_999.99m }).IsValid);
    }

    [Fact]
    public void Amount_above_the_maximum_is_rejected()
    {
        var errors = _validator.Validate(Payment(Date("2026-10-28")) with { Amount = 100_000.00m });

        var message = Assert.Single(errors.ToDictionary()["Amount"]);
        Assert.StartsWith("Betrag darf höchstens 99999", message);
    }

    // Fehlender Test: Verwendungszweck höchstens 100 Zeichen ("".PadRight(n, 'x') ergibt einen Text aus n-mal x)
    [Fact]
    public void Reference_with_100_characters_is_allowed()
    {
        Assert.True(_validator.Validate(Payment(Date("2026-10-28")) with { Reference = "".PadRight(100, 'x') }).IsValid);
    }

    [Fact]
    public void Reference_with_101_characters_is_rejected()
    {
        var errors = _validator.Validate(Payment(Date("2026-10-28")) with { Reference = "".PadRight(101, 'x') });

        Assert.Equal(["Verwendungszweck darf höchstens 100 Zeichen lang sein."], errors.ToDictionary()["Reference"]);
    }

    // Macht aus einem Text wie "-0.01" einen decimal ([InlineData] erlaubt kein decimal), unabhängig von der Spracheinstellung
    private static decimal Amount(string value) => decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

    // Macht aus einem Text wie "2026-10-28" ein DateTime ([InlineData] erlaubt kein DateTime).
    // InvariantCulture: Das Format JJJJ-MM-TT wird unabhängig von der Spracheinstellung des Rechners gelesen
    private static DateTime Date(string value) => DateTime.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

    // Eine "Uhr" für Tests, die immer dieselbe Zeit liefert (wie in BalanceCalculatorTests).
    // TimeProvider ist die .NET-Klasse, über die der Validator "jetzt" abfragt
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        // Liefert immer den im Konstruktor übergebenen Zeitpunkt
        public override DateTimeOffset GetUtcNow() => now;

        // Ortszeit = UTC, damit "heute" im Test nicht von der Zeitzone des Rechners abhängt
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
