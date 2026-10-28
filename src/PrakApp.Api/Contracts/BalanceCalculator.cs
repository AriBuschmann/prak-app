namespace PrakApp.Api.Contracts;

/// <summary>
/// Berechnet, wie viel für einen Vertrag bisher fällig war und wie viel davon bezahlt wurde.
/// </summary>
public sealed class BalanceCalculator(TimeProvider time)
{
    public BalanceInfo Calculate(ContractRecord contract, decimal paidTotal) =>
        Compute(contract.StartDate, contract.EndDate, contract.MonthlyFee, paidTotal, time.GetLocalNow().Date);

    /// <summary>
    /// Beitragskonto zum Stichtag "today". Static, damit auch die Vertragsliste (ContractRepository) dieselbe Rechnung
    /// verwendet, ohne einen ganzen ContractRecord zu haben.
    /// </summary>
    public static BalanceInfo Compute(DateTime start, DateTime? end, decimal monthlyFee, decimal paidTotal, DateTime today)
    {
        var monthsDue = MonthsDue(start, end, today);
        var expected = monthsDue * monthlyFee;
        var balance = expected - paidTotal;
        return new BalanceInfo(monthsDue, expected, paidTotal, balance, IsReminderDue(balance, monthlyFee));
    }

    /// <summary>
    /// Regel aus docs/handbuch/03-arbeitsablaeufe.md (3.5): Liegt der Saldo bei mehr als zwei Monatsbeiträgen,
    /// bekommt der Kunde eine Zahlungserinnerung. Genau zwei Monatsbeiträge Rückstand reichen noch nicht.
    /// </summary>
    public static bool IsReminderDue(decimal balance, decimal monthlyFee) => balance > 2 * monthlyFee;

    /// <summary>
    /// Anzahl der Monatsbeiträge, die bis zum Stichtag fällig geworden sind. Regel (docs/handbuch/02-tarife.md):
    /// Der erste Beitrag ist am Tag des Vertragsbeginns fällig, jeder weitere am selben Kalendertag der Folgemonate.
    /// Nach dem Vertragsende wird nichts mehr fällig.
    /// </summary>
    public static int MonthsDue(DateTime start, DateTime? end, DateTime today)
    {
        var until = end is { } e && e < today ? e : today;
        if (until < start)
        {
            return 0;
        }

        // Ganze Monate seit Vertragsbeginn = Fälligkeitstage nach dem ersten
        var months = (until.Year - start.Year) * 12 + until.Month - start.Month;
        if (until.Day < start.Day)
        {
            months--;
        }
        // + 1 für den ersten Beitrag am Tag des Vertragsbeginns (fehlte vorher, das Konto zählte einen Monat zu wenig)
        return months + 1;
    }
}
