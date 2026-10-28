using PrakApp.Api.Contracts;

namespace PrakApp.Api.Tests.Contracts;

public class BalanceCalculatorTests
{
    private static ContractRecord Contract(DateTime start, DateTime? end = null, decimal fee = 20m) =>
        new(1, "V-2026-0001", 1, "Anna Albers", 1, "Basis-Tarif", 0, 1, start, end, fee, ContractStatus.Active, null, null);

    [Fact]
    public void Nothing_is_due_before_the_contract_starts()
    {
        var months = BalanceCalculator.MonthsDue(new DateTime(2026, 11, 1), null, today: new DateTime(2026, 10, 13));

        Assert.Equal(0, months);
    }

    // Bug: Das Beitragskonto zählte einen Monat zu wenig, weil der erste Beitrag am Tag des Vertragsbeginns fehlte.
    // Regel aus docs/handbuch/02-tarife.md: erster Beitrag am Tag des Vertragsbeginns, jeder weitere am selben
    // Kalendertag der Folgemonate. Alle Tests unten nehmen Vertragsbeginn 15.03.2026, also Fälligkeitstage
    // 15.03., 15.04., 15.05., ... Vor der Korrektur kam bei jedem Fall eine Zahl um 1 zu klein heraus.

    // Wie viele Beiträge sind bis "heute" fällig? "heute" wird hier fest vorgegeben, so hängt der Test nicht vom echten Datum ab.
    // Die Daten stehen als Text in [InlineData], weil dort kein DateTime erlaubt ist (Hilfsmethode Date unten)
    [Theory]
    // Am Tag des Vertragsbeginns: der erste Beitrag ist schon fällig (vorher 0)
    [InlineData("2026-03-15", 1)]
    // Einen Tag vor der zweiten Fälligkeit: weiterhin nur der erste Beitrag
    [InlineData("2026-04-14", 1)]
    // Am Tag der zweiten Fälligkeit: jetzt 2
    [InlineData("2026-04-15", 2)]
    // Mitte Oktober: 15.03. bis 15.09. = 7 Beiträge; der 15.10. ist am 13.10. noch nicht erreicht
    [InlineData("2026-10-13", 7)]
    public void First_fee_is_due_on_the_start_date(string today, int expected)
    {
        // MonthsDue ist static, darum ohne Objekt aufrufbar. null = kein Vertragsende
        var months = BalanceCalculator.MonthsDue(new DateTime(2026, 3, 15), null, Date(today));

        Assert.Equal(expected, months);
    }

    // Nach dem Vertragsende wird nichts mehr fällig. "heute" liegt hier immer weit nach dem Ende (13.10.2026),
    // das Ergebnis hängt also nur vom Vertragsende ab
    [Theory]
    // Ende 20.05.: fällig sind 15.03., 15.04. und 15.05. = 3
    [InlineData("2026-05-20", 3)]
    // Ende genau am Fälligkeitstag 15.05.: der Beitrag an diesem Tag zählt noch mit = 3
    [InlineData("2026-05-15", 3)]
    // Ende am 14.05.: der 15.05. liegt nach dem Ende und zählt nicht mehr = 2
    [InlineData("2026-05-14", 2)]
    public void No_fee_is_due_after_the_end_date(string end, int expected)
    {
        var months = BalanceCalculator.MonthsDue(new DateTime(2026, 3, 15), Date(end), today: new DateTime(2026, 10, 13));

        Assert.Equal(expected, months);
    }

    // Das Beispiel aus dem Handbuch, diesmal über Calculate, also mit Soll und Saldo:
    // Beginn 15.03.2026, Monatsbeitrag 20,00 €, heute 15.03.2026, noch keine Zahlung → 1 Monat, Soll 20 €, Saldo 20 €
    [Fact]
    public void Example_from_the_handbook()
    {
        // Calculate holt "heute" über den TimeProvider. FixedTimeProvider (unten) liefert immer den 15.03.2026, 12 Uhr
        var time = new FixedTimeProvider(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));
        var calculator = new BalanceCalculator(time);

        // Contract(...) ist die Hilfsmethode oben in der Klasse; paidTotal: 0m = noch nichts bezahlt
        var balance = calculator.Calculate(Contract(new DateTime(2026, 3, 15), fee: 20m), paidTotal: 0m);

        // 1 fälliger Monat (vorher 0), Soll = 1 × 20 €, Saldo = Soll − Bezahlt = 20 € (positiv = Rückstand)
        Assert.Equal(1, balance.MonthsDue);
        Assert.Equal(20m, balance.ExpectedTotal);
        Assert.Equal(20m, balance.Balance);
    }

    // Macht aus einem Text wie "2026-03-15" ein DateTime. InvariantCulture: Das Format JJJJ-MM-TT wird unabhängig
    // von der Spracheinstellung des Rechners gelesen
    private static DateTime Date(string value) => DateTime.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void Overpayment_results_in_a_negative_balance()
    {
        var time = new FixedTimeProvider(new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero));
        var calculator = new BalanceCalculator(time);

        var balance = calculator.Calculate(Contract(new DateTime(2026, 12, 1)), paidTotal: 50m);

        Assert.Equal(0m, balance.ExpectedTotal);
        Assert.Equal(-50m, balance.Balance);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
