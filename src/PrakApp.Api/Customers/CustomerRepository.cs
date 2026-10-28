using Dapper;
using PrakApp.Api.Data;

namespace PrakApp.Api.Customers;

public interface ICustomerRepository
{
    Task<IReadOnlyList<CustomerListItem>> SearchAsync(string? query);
    Task<Customer?> GetAsync(int id);
    Task<int> CreateAsync(CustomerInput input, string createdBy);
    Task<bool> UpdateAsync(int id, CustomerInput input);
}

public sealed class CustomerRepository(IDbConnectionFactory db) : ICustomerRepository
{
    public async Task<IReadOnlyList<CustomerListItem>> SearchAsync(string? query)
    {
        // COUNT(co.Id) wurde durch COUNT(DISTINCT co.Id) ersetzt: Der JOIN mit Payment wiederholt jeden Vertrag einmal pro Zahlung,
        // deshalb wird jede Vertrags-Id nur einmal gezählt (sonst käme die Anzahl der Zahlungen heraus).
        const string sql = """
            SELECT cu.Id, cu.CustomerNumber, cu.FirstName, cu.LastName, cu.City,
                   COUNT(DISTINCT co.Id)    AS ContractCount,
                   ISNULL(SUM(p.Amount), 0) AS TotalPaid
            FROM dbo.Customer cu
            LEFT JOIN dbo.Contract co ON co.CustomerId = cu.Id
            LEFT JOIN dbo.Payment p   ON p.ContractId = co.Id
            WHERE @Query IS NULL
               OR cu.LastName LIKE @Query + '%'
               OR cu.FirstName LIKE @Query + '%'
               OR cu.City LIKE @Query + '%'
               OR cu.CustomerNumber LIKE '%' + @Query + '%'
            GROUP BY cu.Id, cu.CustomerNumber, cu.FirstName, cu.LastName, cu.City
            ORDER BY cu.LastName, cu.FirstName;
            """;

        using var connection = db.Create();
        var rows = await connection.QueryAsync<CustomerListItem>(sql, new { Query = Normalize(query) });
        return rows.AsList();
    }

    public async Task<Customer?> GetAsync(int id)
    {
        const string sql = """
            SELECT Id, CustomerNumber, FirstName, LastName, Email, Phone, Street, PostalCode, City, CreatedAt, CreatedBy
            FROM dbo.Customer
            WHERE Id = @id;
            """;

        using var connection = db.Create();
        var customer = await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { id });
        // CreatedAt wird in UTC gespeichert (SYSUTCDATETIME), Dapper weiß das aber nicht. Ohne Kennzeichnung fehlt im JSON
        // das "Z", der Browser liest den Wert als Ortszeit und zeigt nachts zwischen 0 und 2 Uhr das Datum vom Vortag
        return customer is null
            ? null
            : customer with { CreatedAt = DateTime.SpecifyKind(customer.CreatedAt, DateTimeKind.Utc) };
    }

    public async Task<int> CreateAsync(CustomerInput input, string createdBy)
    {
        // Kundennummer: "K-" + laufende Nummer ab 100001
        const string sql = """
            DECLARE @next INT = 100001 + ISNULL((SELECT MAX(Id) FROM dbo.Customer), 0);

            INSERT INTO dbo.Customer (CustomerNumber, FirstName, LastName, Email, Phone, Street, PostalCode, City, CreatedBy)
            VALUES (CONCAT(N'K-', @next), @FirstName, @LastName, @Email, @Phone, @Street, @PostalCode, @City, @CreatedBy);

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        using var connection = db.Create();
        return await connection.ExecuteScalarAsync<int>(sql, Parameters(input, createdBy));
    }

    public async Task<bool> UpdateAsync(int id, CustomerInput input)
    {
        const string sql = """
            UPDATE dbo.Customer
            SET FirstName = @FirstName, LastName = @LastName, Email = @Email, Phone = @Phone,
                Street = @Street, PostalCode = @PostalCode, City = @City
            WHERE Id = @Id;
            """;

        using var connection = db.Create();
        var parameters = Parameters(input, createdBy: null);
        parameters.Add("Id", id);
        return await connection.ExecuteAsync(sql, parameters) == 1;
    }

    private static DynamicParameters Parameters(CustomerInput input, string? createdBy)
    {
        var p = new DynamicParameters();
        p.Add("FirstName", input.FirstName?.Trim());
        p.Add("LastName", input.LastName?.Trim());
        p.Add("Email", Normalize(input.Email));
        p.Add("Phone", Normalize(input.Phone));
        p.Add("Street", Normalize(input.Street));
        p.Add("PostalCode", Normalize(input.PostalCode));
        p.Add("City", Normalize(input.City));
        if (createdBy is not null) p.Add("CreatedBy", createdBy);
        return p;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
