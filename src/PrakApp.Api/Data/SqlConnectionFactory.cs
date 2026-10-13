using System.Data;
using Microsoft.Data.SqlClient;

namespace PrakApp.Api.Data;

public sealed class SqlConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    private readonly string _connectionString = configuration.GetConnectionString("PrakApp")
        ?? throw new InvalidOperationException("Connection string 'PrakApp' fehlt in appsettings.json.");

    public IDbConnection Create() => new SqlConnection(_connectionString);
}
