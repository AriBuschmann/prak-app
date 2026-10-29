using PrakApp.Api.Payments;

namespace PrakApp.Api.Tests.Payments;

// Verbesserung: Zahlungen eines Vertrags als CSV-Datei exportieren. Geprüft wird nur das Format, ohne Datenbank
public class PaymentCsvTests
{
    private const string Header = "Vertragsnummer;Zahlungsdatum;Betrag;Verwendungszweck;Erfasst von\r\n";

    private static Payment Payment(decimal amount, DateTime date, string? reference, string recordedBy = "praktikum") =>
        new(1, 77, amount, date, reference, new DateTime(2026, 10, 28, 8, 0, 0), recordedBy);

    [Fact]
    public void Header_and_one_line_per_payment_in_german_format()
    {
        var csv = PaymentCsv.Build("V-2023-0077", [
            Payment(24.9m, new DateTime(2026, 9, 6), "Beitrag 09/2026", "s.okafor"),
            Payment(1234.5m, new DateTime(2026, 8, 9), null, "t.lindqvist"),
        ]);

        Assert.Equal(
            Header +
            "V-2023-0077;06.09.2026;24,90;Beitrag 09/2026;s.okafor\r\n" +
            "V-2023-0077;09.08.2026;1234,50;;t.lindqvist\r\n",
            csv);
    }

    [Fact]
    public void Without_payments_only_the_header()
    {
        Assert.Equal(Header, PaymentCsv.Build("V-2023-0077", []));
    }

    [Theory]
    [InlineData("Miete; Oktober", "\"Miete; Oktober\"")]
    [InlineData("Beitrag \"Oktober\"", "\"Beitrag \"\"Oktober\"\"\"")]
    [InlineData("Zeile 1\nZeile 2", "\"Zeile 1\nZeile 2\"")]
    public void Text_with_separator_quote_or_line_break_is_quoted(string reference, string expected)
    {
        var csv = PaymentCsv.Build("V-2023-0077", [Payment(9.9m, new DateTime(2026, 10, 1), reference)]);

        Assert.Equal(Header + $"V-2023-0077;01.10.2026;9,90;{expected};praktikum\r\n", csv);
    }

    [Theory]
    [InlineData("=1+2", "'=1+2")]
    [InlineData("+49 30 1234", "'+49 30 1234")]
    [InlineData("-5", "'-5")]
    [InlineData("@SUMME(A1)", "'@SUMME(A1)")]
    [InlineData("=SUMME(1;2)", "\"'=SUMME(1;2)\"")]
    public void Text_that_excel_would_run_as_formula_starts_with_apostrophe(string reference, string expected)
    {
        var csv = PaymentCsv.Build("V-2023-0077", [Payment(9.9m, new DateTime(2026, 10, 1), reference)]);

        Assert.Equal(Header + $"V-2023-0077;01.10.2026;9,90;{expected};praktikum\r\n", csv);
    }
}
