using Microsoft.AspNetCore.Mvc;
using PrakApp.Api.Common;

namespace PrakApp.Api.Contracts;

[ApiController]
[Route("api/contracts")]
public sealed class ContractsController(ContractService service, IContractRepository contracts) : ControllerBase
{
    [HttpGet("/api/contract-types")]
    public Task<IReadOnlyList<ContractType>> GetTypes() => contracts.GetTypesAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContractDetail>> Get(int id) =>
        await service.GetDetailAsync(id) is { } detail ? detail : NotFound();

    [HttpPost]
    public async Task<ActionResult<ContractDetail>> Create(ContractInput input)
    {
        var (id, errors) = await service.CreateAsync(input, User.UserName());
        if (id is null)
        {
            return ValidationProblem(new ValidationProblemDetails(errors.ToDictionary()));
        }

        return CreatedAtAction(nameof(Get), new { id }, await service.GetDetailAsync(id.Value));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancelContractRequest request)
    {
        return await service.CancelAsync(id, request.Reason) switch
        {
            CancelResult.NotFound => NotFound(),
            CancelResult.NotActive => Conflict(new ProblemDetails { Title = "Nur aktive Verträge können gekündigt werden." }),
            _ => NoContent(),
        };
    }
}
