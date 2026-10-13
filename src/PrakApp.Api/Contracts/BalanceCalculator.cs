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

    /// <summary>Anzahl der Monatsbeiträge, die bis zum Stichtag fällig geworden sind.</summary>
    public static int MonthsDue(DateTime start, DateTime? end, DateTime today)
    {
        var until = end is { } e && e < today ? e : today;
        if (until < start)
        {
            return 0;
        }

        var months = (until.Year - start.Year) * 12 + until.Month - start.Month;
        if (until.Day < start.Day)
        {
            months--;
        }
        return months;
    }
}
