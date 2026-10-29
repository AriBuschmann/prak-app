using Microsoft.AspNetCore.Mvc;
using PrakApp.Api.Common;
using PrakApp.Api.Contracts;

namespace PrakApp.Api.Customers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(
    ICustomerRepository customers,
    IContractRepository contracts,
    CustomerValidator validator) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CustomerListItem>> Search([FromQuery] string? q) => customers.SearchAsync(q);

    // Warnung vor doppelten Kunden: Das Frontend fragt das vor dem Anlegen ab. Ohne Vor- oder Nachnamen keine Treffer
    [HttpGet("duplicates")]
    public async Task<IReadOnlyList<CustomerDuplicate>> FindDuplicates(
        [FromQuery] string? firstName, [FromQuery] string? lastName, [FromQuery] string? postalCode) =>
        string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName)
            ? []
            : await customers.FindDuplicatesAsync(firstName, lastName, postalCode);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Customer>> Get(int id) =>
        await customers.GetAsync(id) is { } customer ? customer : NotFound();

    [HttpGet("{id:int}/contracts")]
    public async Task<ActionResult<IReadOnlyList<ContractListItem>>> GetContracts(int id)
    {
        if (await customers.GetAsync(id) is null)
        {
            return NotFound();
        }
        return Ok(await contracts.GetByCustomerAsync(id));
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> Create(CustomerInput input)
    {
        var errors = validator.Validate(input);
        if (!errors.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(errors.ToDictionary()));
        }

        var id = await customers.CreateAsync(input, User.UserName());
        return CreatedAtAction(nameof(Get), new { id }, await customers.GetAsync(id));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Customer>> Update(int id, CustomerInput input)
    {
        var errors = validator.Validate(input);
        if (!errors.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(errors.ToDictionary()));
        }

        if (!await customers.UpdateAsync(id, input))
        {
            return NotFound();
        }
        return await customers.GetAsync(id) is { } customer ? customer : NotFound();
    }

    // Nur Kunden ohne Verträge, denn Verträge und Zahlungen müssen erhalten bleiben (siehe api.md)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (await customers.GetAsync(id) is null)
        {
            return NotFound();
        }
        if (!await customers.DeleteAsync(id))
        {
            return Conflict(new ProblemDetails { Title = "Kunden mit Verträgen können nicht gelöscht werden." });
        }
        return NoContent();
    }
}
