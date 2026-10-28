using Dapper;
using PrakApp.Api.Data;

namespace PrakApp.Api.Contracts;

public interface IContractRepository
{
    Task<IReadOnlyList<ContractType>> GetTypesAsync();
    Task<IReadOnlyList<ContractListItem>> GetByCustomerAsync(int customerId);
    Task<IReadOnlyList<ContractSearchItem>> SearchByNumberAsync(string query);
    Task<ContractRecord?> GetAsync(int id);
    Task<int> CreateAsync(ContractInput input, string createdBy);
    Task CancelAsync(int id, DateTime cancelledAt, DateTime endDate, string reason);
}

// TimeProvider liefert "heute" für den Status "Beendet" (ContractStatus.Effective)
public sealed class ContractRepository(IDbConnectionFactory db, TimeProvider time) : IContractRepository
{
    public async Task<IReadOnlyList<ContractType>> GetTypesAsync()
    {
        using var connection = db.Create();
        var rows = await connection.QueryAsync<ContractType>(
            "SELECT Id, Code, Name, MinTermMonths, NoticePeriodMonths FROM dbo.ContractType ORDER BY Id;");
        return rows.AsList();
    }

    public async Task<IReadOnlyList<ContractListItem>> GetByCustomerAsync(int customerId)
    {
        const string sql = """
            SELECT c.Id, c.ContractNumber, t.Name AS TypeName, c.StartDate, c.EndDate, c.MonthlyFee, c.Status
            FROM dbo.Contract c
            JOIN dbo.ContractType t ON t.Id = c.ContractTypeId
            WHERE c.CustomerId = @customerId
            ORDER BY c.StartDate DESC;
            """;

        using var connection = db.Create();
        var rows = await connection.QueryAsync<ContractListItem>(sql, new { customerId });
        // Verträge mit vergangenem Vertragsende als "Ended" liefern (siehe ContractStatus.Effective)
        var today = time.GetLocalNow().Date;
        return rows.Select(c => c with { Status = ContractStatus.Effective(c.Status, c.EndDate, today) }).ToList();
    }

    // Verträge, deren Nummer den Suchtext enthält (wie bei der Kundennummer), höchstens 10 Treffer.
    // Groß-/Kleinschreibung spielt keine Rolle, weil die Datenbank Texte so vergleicht (Sortierfolge "CI")
    public async Task<IReadOnlyList<ContractSearchItem>> SearchByNumberAsync(string query)
    {
        const string sql = """
            SELECT TOP 10 c.Id, c.ContractNumber, CONCAT(cu.FirstName, N' ', cu.LastName) AS CustomerName, c.EndDate, c.Status
            FROM dbo.Contract c
            JOIN dbo.Customer cu ON cu.Id = c.CustomerId
            WHERE c.ContractNumber LIKE '%' + @Query + '%'
            ORDER BY c.ContractNumber;
            """;

        using var connection = db.Create();
        var rows = await connection.QueryAsync<ContractSearchItem>(sql, new { Query = SqlLike.Escape(query.Trim()) });
        var today = time.GetLocalNow().Date;
        return rows.Select(c => c with { Status = ContractStatus.Effective(c.Status, c.EndDate, today) }).ToList();
    }

    public async Task<ContractRecord?> GetAsync(int id)
    {
        const string sql = """
            SELECT c.Id, c.ContractNumber, c.CustomerId, CONCAT(cu.FirstName, N' ', cu.LastName) AS CustomerName,
                   c.ContractTypeId, t.Name AS TypeName, t.MinTermMonths, t.NoticePeriodMonths,
                   c.StartDate, c.EndDate, c.MonthlyFee, c.Status, c.CancelledAt, c.CancellationReason
            FROM dbo.Contract c
            JOIN dbo.Customer cu    ON cu.Id = c.CustomerId
            JOIN dbo.ContractType t ON t.Id = c.ContractTypeId
            WHERE c.Id = @id;
            """;

        using var connection = db.Create();
        var contract = await connection.QuerySingleOrDefaultAsync<ContractRecord>(sql, new { id });
        // Wie oben: nach dem Vertragsende "Ended". Weil auch Kündigung und Kündigungsvorschau den Vertrag hierüber
        // lesen, lehnt der ContractService das Kündigen eines beendeten Vertrags automatisch ab (409)
        return contract is null
            ? null
            : contract with { Status = ContractStatus.Effective(contract.Status, contract.EndDate, time.GetLocalNow().Date) };
    }

    public async Task<int> CreateAsync(ContractInput input, string createdBy)
    {
        // Vertragsnummer: V-<Jahr des Vertragsbeginns>-<laufende Nummer>
        // Laufende Nummer aus dem Zähler ContractNumberSeq (Patch 1.1.0/0003) statt MAX(Id) + 1, siehe CustomerRepository
        const string sql = """
            DECLARE @next INT = NEXT VALUE FOR dbo.ContractNumberSeq;

            INSERT INTO dbo.Contract (ContractNumber, CustomerId, ContractTypeId, StartDate, EndDate, MonthlyFee, CreatedBy)
            VALUES (CONCAT(N'V-', YEAR(@StartDate), N'-', RIGHT(CONCAT(N'0000', @next), 4)),
                    @CustomerId, @ContractTypeId, @StartDate, @EndDate, @MonthlyFee, @CreatedBy);

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        using var connection = db.Create();
        return await connection.ExecuteScalarAsync<int>(sql, new
        {
            input.CustomerId,
            input.ContractTypeId,
            input.StartDate,
            input.EndDate,
            input.MonthlyFee,
            CreatedBy = createdBy,
        });
    }

    // Neu (Schritt 3): speichert jetzt auch das berechnete Vertragsende (EndDate)
    public async Task CancelAsync(int id, DateTime cancelledAt, DateTime endDate, string reason)
    {
        const string sql = """
            UPDATE dbo.Contract
            SET Status = N'Cancelled', CancelledAt = @cancelledAt, CancellationReason = @reason, EndDate = @endDate
            WHERE Id = @id;
            """;

        using var connection = db.Create();
        await connection.ExecuteAsync(sql, new { id, cancelledAt, endDate, reason });
    }
}
