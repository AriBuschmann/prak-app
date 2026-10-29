using PrakApp.Api.Contracts;

namespace PrakApp.Api.Tests.Contracts;

public class ContractServiceUndoTests
{
    private static readonly DateTime Today = new(2026, 4, 13);

    private static ContractRecord Contract(string status = ContractStatus.Cancelled, bool undoable = true) =>
        new(7, "V-2026-0007", 1, "Anna Albers", 2, "Komfort-Tarif", MinTermMonths: 12, NoticePeriodMonths: 3,
            StartDate: new DateTime(2026, 3, 1), EndDate: new DateTime(2027, 2, 28), MonthlyFee: 24.90m, Status: status,
            CancelledAt: new DateTime(2026, 4, 1), CancellationReason: "Umzug",
            CancellationUndoable: undoable, EndDateBeforeCancellation: null);

    private static ContractService Service(FakeContractRepository contracts) =>
        new(contracts, customers: null!, payments: null!, new ContractValidator(), new CancellationValidator(),
            new BalanceCalculator(new FixedTimeProvider(Today)), new FixedTimeProvider(Today));

    [Fact]
    public async Task Cancellation_is_undone()
    {
        var repository = new FakeContractRepository(Contract());

        var result = await Service(repository).UndoCancellationAsync(7);

        Assert.Equal(UndoCancellationResult.Undone, result);
        Assert.Equal(7, repository.UndoneId);
    }

    [Fact]
    public async Task Unknown_contract_is_not_found()
    {
        var repository = new FakeContractRepository(Contract());

        var result = await Service(repository).UndoCancellationAsync(8);

        Assert.Equal(UndoCancellationResult.NotFound, result);
        Assert.Null(repository.UndoneId);
    }

    [Theory]
    [InlineData(ContractStatus.Active, UndoCancellationResult.NotCancelled)]
    [InlineData(ContractStatus.Ended, UndoCancellationResult.AlreadyEnded)]
    public async Task Only_cancelled_contracts_that_have_not_ended_can_be_undone(string status, UndoCancellationResult expected)
    {
        var repository = new FakeContractRepository(Contract(status));

        var result = await Service(repository).UndoCancellationAsync(7);

        Assert.Equal(expected, result);
        Assert.Null(repository.UndoneId);
    }

    [Fact]
    public async Task Cancellation_without_remembered_end_date_cannot_be_undone()
    {
        var repository = new FakeContractRepository(Contract(undoable: false));

        var result = await Service(repository).UndoCancellationAsync(7);

        Assert.Equal(UndoCancellationResult.NotUndoable, result);
        Assert.Null(repository.UndoneId);
    }

    private sealed class FakeContractRepository(ContractRecord contract) : IContractRepository
    {
        public int? UndoneId { get; private set; }

        public Task<ContractRecord?> GetAsync(int id) => Task.FromResult(id == contract.Id ? contract : null);

        public Task<bool> UndoCancellationAsync(int id)
        {
            UndoneId = id;
            return Task.FromResult(true);
        }

        public Task CancelAsync(int id, DateTime cancelledAt, DateTime endDate, string reason) => throw new NotSupportedException();
        public Task<IReadOnlyList<ContractType>> GetTypesAsync() => throw new NotSupportedException();
        public Task<IReadOnlyList<ContractListItem>> GetByCustomerAsync(int customerId) => throw new NotSupportedException();
        public Task<IReadOnlyList<ContractSearchItem>> SearchByNumberAsync(string query) => throw new NotSupportedException();
        public Task<int> CreateAsync(ContractInput input, string createdBy) => throw new NotSupportedException();
    }

    private sealed class FixedTimeProvider(DateTime today) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(today.AddHours(12), TimeSpan.Zero);

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
