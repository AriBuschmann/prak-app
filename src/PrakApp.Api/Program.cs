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
app.MapFallbackToFile("index.html");

app.Run();

// Für Integrationstests (WebApplicationFactory<Program>)
public partial class Program;
