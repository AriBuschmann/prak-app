using PrakApp.Api.Common;
using PrakApp.Api.Customers;
using PrakApp.Api.Payments;

namespace PrakApp.Api.Contracts;

public enum CancelResult
{
    Cancelled,
    NotFound,
    NotActive,
}

public sealed class ContractService(
    IContractRepository contracts,
    ICustomerRepository customers,
    IPaymentRepository payments,
    ContractValidator validator,
    BalanceCalculator balanceCalculator,
    TimeProvider time)
{
    public async Task<ContractDetail?> GetDetailAsync(int id)
    {
        var contract = await contracts.GetAsync(id);
        if (contract is null)
        {
            return null;
        }

        var paid = await payments.GetTotalAsync(id);
        return new ContractDetail(contract, balanceCalculator.Calculate(contract, paid));
    }

    public async Task<(int? Id, ValidationErrors Errors)> CreateAsync(ContractInput input, string createdBy)
    {
        var errors = validator.Validate(input);
        if (errors.IsValid)
        {
            if (await customers.GetAsync(input.CustomerId) is null)
                errors.Add(nameof(input.CustomerId), "Kunde existiert nicht.");

            var types = await contracts.GetTypesAsync();
            if (types.All(t => t.Id != input.ContractTypeId))
                errors.Add(nameof(input.ContractTypeId), "Vertragsart existiert nicht.");
        }

        if (!errors.IsValid)
        {
            return (null, errors);
        }

        return (await contracts.CreateAsync(input, createdBy), errors);
    }

    public async Task<CancelResult> CancelAsync(int id, string? reason)
    {
        var contract = await contracts.GetAsync(id);
        if (contract is null)
        {
            return CancelResult.NotFound;
        }
        if (contract.Status != ContractStatus.Active)
        {
            return CancelResult.NotActive;
        }

        // TODO: Kündigungsfrist und Mindestlaufzeit prüfen und das Vertragsende berechnen
        //       (siehe docs/features/kuendigung.md). Bis dahin wird nur der Status gesetzt.
        var today = time.GetLocalNow().Date;
        await contracts.CancelAsync(id, today, reason);
        return CancelResult.Cancelled;
    }
}
