using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace PrakApp.Api.Auth;

/// <summary>
/// Demo-Anmeldung ohne Passwort: Man wählt einen Benutzer aus der Liste.
/// Nur für das Übungsprojekt gedacht – das ist keine echte Sicherheit!
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(IOptions<DemoLoginOptions> options) : ControllerBase
{
    public sealed record LoginRequest(string UserName);

    public sealed record UserInfo(string UserName, string DisplayName, string Role);

    [HttpGet("users")]
    public IEnumerable<UserInfo> GetUsers() =>
        options.Value.Users.Select(u => new UserInfo(u.UserName, u.DisplayName, u.Role));

    [HttpPost("login")]
    public async Task<ActionResult<UserInfo>> Login(LoginRequest request)
    {
        var user = options.Value.Users.FirstOrDefault(u =>
            string.Equals(u.UserName, request.UserName, StringComparison.OrdinalIgnoreCase));
        if (user is null)
        {
            return Unauthorized();
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("display_name", user.DisplayName),
                new Claim(ClaimTypes.Role, user.Role),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return new UserInfo(user.UserName, user.DisplayName, user.Role);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    public ActionResult<UserInfo> Me()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        return new UserInfo(
            User.FindFirstValue(ClaimTypes.Name)!,
            User.FindFirstValue("display_name") ?? "",
            User.FindFirstValue(ClaimTypes.Role) ?? "");
    }
}
