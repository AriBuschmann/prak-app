using System.Text;
using Microsoft.AspNetCore.Mvc;
using PrakApp.Api.Common;
using PrakApp.Api.Contracts;

namespace PrakApp.Api.Payments;

[ApiController]
[Route("api/contracts/{contractId:int}/payments")]
public sealed class PaymentsController(
    IPaymentRepository payments,
    IContractRepository contracts,
    PaymentValidator validator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Payment>>> GetAll(int contractId)
    {
        if (await contracts.GetAsync(contractId) is null)
        {
            return NotFound();
        }
        return Ok(await payments.GetByContractAsync(contractId));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(int contractId)
    {
        var contract = await contracts.GetAsync(contractId);
        if (contract is null)
        {
            return NotFound();
        }

        var csv = PaymentCsv.Build(contract.ContractNumber, await payments.GetByContractAsync(contractId));
        // Mit BOM (unsichtbare Kennung am Dateianfang), sonst liest Excel die Datei nicht als UTF-8 und zeigt Umlaute falsch
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"zahlungen-{contract.ContractNumber}.csv");
    }

    [HttpPost]
    public async Task<IActionResult> Create(int contractId, PaymentInput input)
    {
        if (await contracts.GetAsync(contractId) is null)
        {
            return NotFound();
        }

        var errors = validator.Validate(input);
        if (!errors.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(errors.ToDictionary()));
        }

        var id = await payments.CreateAsync(contractId, input, User.UserName());
        return Created($"/api/contracts/{contractId}/payments", new { id });
    }
}
