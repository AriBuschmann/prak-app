namespace PrakApp.Api.Contracts;

/// <summary>
/// Berechnet, wie viel für einen Vertrag bisher fällig war und wie viel davon bezahlt wurde.
/// </summary>
public sealed class BalanceCalculator(TimeProvider time)
{
    public BalanceInfo Calculate(ContractRecord contract, decimal paidTotal)
    {
        var today = time.GetLocalNow().Date;
        var monthsDue = MonthsDue(contract.StartDate, contract.EndDate, today);
        var expected = monthsDue * contract.MonthlyFee;
        return new BalanceInfo(monthsDue, expected, paidTotal, expected - paidTotal);
    }

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
