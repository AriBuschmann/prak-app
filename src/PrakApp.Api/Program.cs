using Microsoft.AspNetCore.Authentication.Cookies;
using PrakApp.Api.Auth;
using PrakApp.Api.Contracts;
using PrakApp.Api.Customers;
using PrakApp.Api.Data;
using PrakApp.Api.Payments;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// Demo-Anmeldung über ein Cookie (siehe AuthController)
builder.Services.Configure<DemoLoginOptions>(builder.Configuration.GetSection(DemoLoginOptions.Section));
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "prakapp.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        // Für eine API keine Weiterleitung auf eine Login-Seite, sondern einfach 401/403
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

// Dependency Injection: Infrastruktur, Repositories, Validierung, Services
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

builder.Services.AddSingleton<CustomerValidator>();
builder.Services.AddSingleton<ContractValidator>();
// Neu (Schritt 2): Validator für den Kündigungsgrund. Singleton wie die anderen Validatoren, weil er keinen Zustand hat,
// ohne diese Zeile könnte der DI-Container den ContractService nicht erzeugen
builder.Services.AddSingleton<CancellationValidator>();
builder.Services.AddSingleton<PaymentValidator>();
builder.Services.AddSingleton<BalanceCalculator>();
builder.Services.AddScoped<ContractService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Das gebaute Frontend (pnpm build) liegt in wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers().RequireAuthorization();
// Unbekannte API-Adressen: 404 statt der Startseite. Gilt für alle HTTP-Methoden (GET, POST, ...).
// Die Route /api/... ist genauer als der Fallback darunter und gewinnt deshalb bei allen Adressen, die mit /api/ beginnen
app.MapFallback("/api/{**path}", () => Results.NotFound());
// Alle anderen unbekannten Adressen (z. B. /customers/40 nach F5) bekommen index.html, den Rest erledigt der Aurelia-Router
app.MapFallbackToFile("index.html");

app.Run();

// Für Integrationstests (WebApplicationFactory<Program>)
public partial class Program;
