using System.Globalization;
using PrakApp.Api.Contracts;

namespace PrakApp.Api.Tests.Contracts;

public class CancellationCalculatorTests
{
    // Hilfsmethode: macht aus "2026-10-13" ein DateTime. InvariantCulture sorgt dafür, dass das Format
    // JJJJ-MM-TT unabhängig von den Spracheinstellungen des Rechners gleich verstanden wird.
    private static DateTime D(string isoDate) => DateTime.Parse(isoDate, CultureInfo.InvariantCulture);

    // Beispiel aus Regel 4: Kündigung 13.10.2026, Frist 3 Monate → Fristablauf 13.01.2027 → Ende 31.01.2027
    // [Fact] = ein einzelner Testfall mit festen Werten
    [Fact]
    public void Contract_ends_at_the_end_of_the_month_in_which_the_notice_period_expires()
    {
        // Ausführen: Vertragsende berechnen. Die Namen vor dem Doppelpunkt (cancelledAt: …) sind nur zur
        // besseren Lesbarkeit; sie zeigen, welcher Wert zu welchem Parameter gehört.
        var end = CancellationCalculator.EndDate(D("2024-01-15"), cancelledAt: D("2026-10-13"), noticePeriodMonths: 3, minTermMonths: 0);

        // Prüfen: Assert.Equal(erwartet, tatsächlich), bei Abweichung schlägt der Test fehl und zeigt beide Werte
        Assert.Equal(D("2027-01-31"), end);
    }

    // Beispiel aus Regel 5: Beginn 01.03.2026, Mindestlaufzeit 12 Monate → frühestes Ende 28.02.2027.
    // Nach Regel 4 allein wäre schon am 31.07.2026 Schluss (13.04. + 3 Monate).
    [Fact]
    public void Contract_does_not_end_before_the_minimum_term()
    {
        var end = CancellationCalculator.EndDate(D("2026-03-01"), cancelledAt: D("2026-04-13"), noticePeriodMonths: 3, minTermMonths: 12);

        Assert.Equal(D("2027-02-28"), end);
    }

    // Mindestlaufzeit ist längst vorbei → nur Regel 4 zählt
    [Fact]
    public void Expired_minimum_term_does_not_change_the_end_date()
    {
        var end = CancellationCalculator.EndDate(D("2023-02-26"), cancelledAt: D("2026-10-22"), noticePeriodMonths: 3, minTermMonths: 24);

        Assert.Equal(D("2027-01-31"), end);
    }

    // Tarif BASIS: keine Mindestlaufzeit, 1 Monat Frist, auch wenn der Vertrag erst heute begonnen hat
    [Fact]
    public void Contract_without_minimum_term_can_end_after_the_notice_period()
    {
        var end = CancellationCalculator.EndDate(D("2026-10-22"), cancelledAt: D("2026-10-22"), noticePeriodMonths: 1, minTermMonths: 0);

        Assert.Equal(D("2026-11-30"), end);
    }

    // Sonderfälle für Regel 4: Monatsenden, Jahreswechsel, Schaltjahr
    // [Theory] = derselbe Test läuft einmal pro [InlineData]-Zeile; die Werte werden der Reihe nach
    // an die Parameter der Methode übergeben (cancelledAt, noticePeriodMonths, expectedEnd)
    [Theory]
    [InlineData("2026-10-31", 1, "2026-11-30")] // 31.10. + 1 Monat: den 31.11. gibt es nicht → Ende November
    [InlineData("2026-12-31", 3, "2027-03-31")] // über den Jahreswechsel
    [InlineData("2027-11-30", 3, "2028-02-29")] // Fristablauf im Februar eines Schaltjahrs
    [InlineData("2026-11-30", 3, "2027-02-28")] // Fristablauf im Februar eines normalen Jahrs
    [InlineData("2026-10-01", 1, "2026-11-30")] // Kündigung am Monatsersten
    public void Notice_period_edge_cases(string cancelledAt, int noticePeriodMonths, string expectedEnd)
    {
        // Beginn weit in der Vergangenheit und keine Mindestlaufzeit: So zählt hier nur Regel 4
        var end = CancellationCalculator.EndDate(D("2020-01-01"), D(cancelledAt), noticePeriodMonths, minTermMonths: 0);

        Assert.Equal(D(expectedEnd), end);
    }

    // Sonderfälle für Regel 5: letzter Tag der Mindestlaufzeit = Tag vor dem gleichen Datum n Monate später,
    // gibt es diesen Tag im Zielmonat nicht, endet sie am letzten Tag dieses Monats
    [Theory]
    [InlineData("2026-03-01", 12, "2027-02-28")] // Beispiel aus kuendigung.md
    [InlineData("2026-01-31", 12, "2027-01-30")] // Beginn am 31.: Ende am 30. ein Jahr später
    [InlineData("2028-02-29", 12, "2029-02-28")] // Beginn am Schalttag: den 29.02.2029 gibt es nicht
    [InlineData("2026-05-31", 24, "2028-05-30")] // PREMIUM: 24 Monate
    public void Minimum_term_edge_cases(string startDate, int minTermMonths, string expectedEnd)
    {
        // Kündigung direkt am ersten Tag mit 1 Monat Frist, damit die Mindestlaufzeit bestimmt
        var end = CancellationCalculator.EndDate(D(startDate), D(startDate), noticePeriodMonths: 1, minTermMonths);

        Assert.Equal(D(expectedEnd), end);
    }

    // Uhrzeiten dürfen das Ergebnis nicht beeinflussen
    [Fact]
    public void Time_of_day_is_ignored()
    {
        var end = CancellationCalculator.EndDate(D("2024-01-15T08:30:00"), cancelledAt: D("2026-10-13T17:45:00"), noticePeriodMonths: 3, minTermMonths: 0);

        Assert.Equal(D("2027-01-31"), end);
    }

    // Wie oben, aber hier entscheidet die Mindestlaufzeit (Regel 5): Auch dann darf die Uhrzeit
    // des Vertragsbeginns (08:30) nicht im Ergebnis landen, erwartet wird 28.02.2027 um 00:00 Uhr
    [Fact]
    public void Time_of_day_is_ignored_when_the_minimum_term_decides()
    {
        var end = CancellationCalculator.EndDate(D("2026-03-01T08:30:00"), cancelledAt: D("2026-04-13T17:45:00"), noticePeriodMonths: 3, minTermMonths: 12);

        Assert.Equal(D("2027-02-28"), end);
    }

    // ---- Neu in Schritt 2: Tests für die Regeln 8 und 9 (die drei Tests ab hier) ----

    // Befristeter Vertrag: Ein vorhandenes Vertragsende hat Vorrang, wenn es früher liegt.
    // Liegt es später, gilt das berechnete Ende. Die Kündigung beendet den Vertrag dann früher.
    // Berechnetes Ende hier jeweils 31.01.2027 (Kündigung 13.10.2026, 3 Monate Frist, keine Mindestlaufzeit)
    [Theory]
    [InlineData("2026-12-31", "2026-12-31")] // vorhandenes Ende früher → bleibt
    [InlineData("2027-01-31", "2027-01-31")] // genau gleich
    [InlineData("2027-06-30", "2027-01-31")] // vorhandenes Ende später → berechnetes Ende gilt
    public void Existing_end_date_wins_if_it_is_earlier(string existingEndDate, string expectedEnd)
    {
        var end = CancellationCalculator.EndDate(D("2024-01-15"), cancelledAt: D("2026-10-13"), noticePeriodMonths: 3, minTermMonths: 0,
            existingEndDate: D(existingEndDate));

        Assert.Equal(D(expectedEnd), end);
    }

    // Kündigung vor Vertragsbeginn (Entscheidung Betreuer): erlaubt, die Mindestlaufzeit gilt trotzdem.
    // KOMFORT: Beginn 01.12.2026, 12 Monate Mindestlaufzeit → frühestens 30.11.2027
    [Fact]
    public void Contract_cancelled_before_it_starts_still_has_the_minimum_term()
    {
        var end = CancellationCalculator.EndDate(D("2026-12-01"), cancelledAt: D("2026-10-22"), noticePeriodMonths: 3, minTermMonths: 12);

        Assert.Equal(D("2027-11-30"), end);
    }

    // Annahme: Ohne Mindestlaufzeit (BASIS) könnte das berechnete Ende vor dem Beginn liegen (hier 30.11.2026).
    // Ein Vertragsende vor dem Beginn ist nicht möglich (Datenbankregel EndDate >= StartDate),
    // deshalb endet der Vertrag dann am Tag seines Beginns.
    [Fact]
    public void Contract_never_ends_before_it_starts()
    {
        var end = CancellationCalculator.EndDate(D("2026-12-01"), cancelledAt: D("2026-10-22"), noticePeriodMonths: 1, minTermMonths: 0);

        Assert.Equal(D("2026-12-01"), end);
    }
}
