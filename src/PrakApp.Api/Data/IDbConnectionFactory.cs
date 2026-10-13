using System.Data;

namespace PrakApp.Api.Data;

public interface IDbConnectionFactory
{
    IDbConnection Create();
}
