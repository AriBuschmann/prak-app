using Dapper;
using PrakApp.Api.Data;

namespace PrakApp.Api.Payments;

public interface IPaymentRepository
{
    Task<IReadOnlyList<Payment>> GetByContractAsync(int contractId);
    Task<decimal> GetTotalAsync(int contractId);
    Task<int> CreateAsync(int contractId, PaymentInput input, string recordedBy);
}

public sealed class PaymentRepository(IDbConnectionFactory db) : IPaymentRepository
{
    public async Task<IReadOnlyList<Payment>> GetByContractAsync(int contractId)
    {
        const string sql = """
            SELECT Id, ContractId, Amount, PaymentDate, Reference, RecordedAt, RecordedBy
            FROM dbo.Payment
            WHERE ContractId = @contractId
            ORDER BY PaymentDate DESC, Id DESC;
            """;

        using var connection = db.Create();
        var rows = await connection.QueryAsync<Payment>(sql, new { contractId });
        return rows.AsList();
    }

    public async Task<decimal> GetTotalAsync(int contractId)
    {
        using var connection = db.Create();
        return await connection.ExecuteScalarAsync<decimal>(
            "SELECT ISNULL(SUM(Amount), 0) FROM dbo.Payment WHERE ContractId = @contractId;",
            new { contractId });
    }

    public async Task<int> CreateAsync(int contractId, PaymentInput input, string recordedBy)
    {
        const string sql = """
            INSERT INTO dbo.Payment (ContractId, Amount, PaymentDate, Reference, RecordedBy)
            VALUES (@ContractId, @Amount, @PaymentDate, @Reference, @RecordedBy);

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        using var connection = db.Create();
        return await connection.ExecuteScalarAsync<int>(sql, new
        {
            ContractId = contractId,
            input.Amount,
            input.PaymentDate,
            Reference = string.IsNullOrWhiteSpace(input.Reference) ? null : input.Reference.Trim(),
            RecordedBy = recordedBy,
        });
    }
}
