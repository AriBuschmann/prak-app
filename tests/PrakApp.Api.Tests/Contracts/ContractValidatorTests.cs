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
}
