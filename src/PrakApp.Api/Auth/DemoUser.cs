namespace PrakApp.Api.Auth;

public sealed class DemoLoginOptions
{
    public const string Section = "DemoLogin";

    public List<DemoUser> Users { get; set; } = [];
}

public sealed class DemoUser
{
    public string UserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "";
}
