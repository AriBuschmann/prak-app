using System.Security.Claims;

namespace PrakApp.Api.Common;

public static class CurrentUser
{
    public static string UserName(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Name) ?? "unbekannt";
}
