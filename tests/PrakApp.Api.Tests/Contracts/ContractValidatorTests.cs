using PrakApp.Api.Contracts;

namespace PrakApp.Api.Tests.Contracts;

public class ContractValidatorTests
{
    private readonly ContractValidator _validator = new();

    private static ContractInput ValidContract() =>
        new(CustomerId: 1, ContractTypeId: 2, StartDate: new DateTime(2026, 11, 1), EndDate: null, MonthlyFee: 24.90m);

    [Fact]
    public void Valid_contract_has_no_errors()
    {
        Assert.True(_validator.Validate(ValidContract()).IsValid);
    }

    [Fact]
    public void End_date_before_start_date_is_rejected()
    {
        var errors = _validator.Validate(ValidContract() with { EndDate = new DateTime(2026, 10, 31) });

        Assert.Contains("EndDate", errors.ToDictionary().Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Monthly_fee_must_be_positive(decimal fee)
    {
        var errors = _validator.Validate(ValidContract() with { MonthlyFee = fee });

        Assert.Contains("MonthlyFee", errors.ToDictionary().Keys);
    }

    [Fact]
    public void Start_date_is_required()
    {
        var errors = _validator.Validate(ValidContract() with { StartDate = null });

        Assert.Contains("StartDate", errors.ToDictionary().Keys);
    }

    // Erlaubter Zeitraum für Vertragsbeginn und -ende: 01.01.2000 bis 31.12.2099 (ContractValidator.MinDate/MaxDate).
    // Bug: Ein zweistellig getipptes Jahr ("26") wird im Datumsfeld des Browsers zum Jahr 0026 und führte zu Fehler 500.

    // Grenzwert-Test: Der erste und der letzte erlaubte Tag sind gültig.
    // [InlineData] kann kein DateTime enthalten (nur feste Werte wie Zahlen oder Texte),
    // darum werden Jahr, Monat und Tag einzeln übergeben und unten zu einem Datum zusammengesetzt
    [Theory]
    [InlineData(2000, 1, 1)]
    [InlineData(2099, 12, 31)]
    public void Dates_at_the_limits_are_allowed(int year, int month, int day)
    {
        var date = new DateTime(year, month, day);

        // Beginn und Ende auf denselben Tag setzen: So werden beide Felder geprüft, und das Ende liegt nicht vor dem Beginn
        Assert.True(_validator.Validate(ValidContract() with { StartDate = date, EndDate = date }).IsValid);
    }

    // Vertragsbeginn außerhalb des Zeitraums wird abgelehnt:
    // Jahr 26 (der eigentliche Bug), ein Tag vor der Untergrenze und ein Tag nach der Obergrenze
    [Theory]
    [InlineData(26, 10, 1)]
    [InlineData(1999, 12, 31)]
    [InlineData(2100, 1, 1)]
    public void Start_date_outside_the_allowed_range_is_rejected(int year, int month, int day)
    {
        var errors = _validator.Validate(ValidContract() with { StartDate = new DateTime(year, month, day) });

        // Assert.Equal mit einer Liste ["..."] prüft zweierlei: Es gibt genau eine Meldung bei StartDate,
        // und sie hat genau diesen Text (so ist auch das Datumsformat 01.01.2000 geprüft)
        Assert.Equal(["Vertragsbeginn muss zwischen 01.01.2000 und 31.12.2099 liegen."], errors.ToDictionary()["StartDate"]);
    }

    // Vertragsende außerhalb des Zeitraums wird abgelehnt (der Vertragsbeginn aus ValidContract() ist der 01.11.2026)
    [Theory]
    [InlineData(26, 10, 1)]
    [InlineData(2100, 1, 1)]
    public void End_date_outside_the_allowed_range_is_rejected(int year, int month, int day)
    {
        var errors = _validator.Validate(ValidContract() with { EndDate = new DateTime(year, month, day) });

        // Genau eine Meldung: Das Jahr 26 liegt auch vor dem Vertragsbeginn, trotzdem darf nicht zusätzlich
        // "Vertragsende darf nicht vor dem Vertragsbeginn liegen." erscheinen (vorher kam nur diese, irreführende Meldung)
        Assert.Equal(["Vertragsende muss zwischen 01.01.2000 und 31.12.2099 liegen."], errors.ToDictionary()["EndDate"]);
    }
}
