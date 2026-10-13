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
