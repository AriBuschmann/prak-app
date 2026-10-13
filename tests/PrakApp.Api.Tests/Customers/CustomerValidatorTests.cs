using PrakApp.Api.Customers;

namespace PrakApp.Api.Tests.Customers;

public class CustomerValidatorTests
{
    private readonly CustomerValidator _validator = new();

    private static CustomerInput ValidCustomer() =>
        new("Anna", "Albers", "anna.albers@example.org", "0431 123456", "Lindenweg 3", "24103", "Kiel");

    [Fact]
    public void Valid_customer_has_no_errors()
    {
        var errors = _validator.Validate(ValidCustomer());

        Assert.True(errors.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void First_name_is_required(string? firstName)
    {
        var errors = _validator.Validate(ValidCustomer() with { FirstName = firstName });

        Assert.False(errors.IsValid);
        Assert.Contains("FirstName", errors.ToDictionary().Keys);
    }

    [Fact]
    public void Last_name_is_required()
    {
        var errors = _validator.Validate(ValidCustomer() with { LastName = "" });

        Assert.Contains("LastName", errors.ToDictionary().Keys);
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("123456")]
    [InlineData("24l03")]
    public void Postal_code_must_have_five_digits(string postalCode)
    {
        var errors = _validator.Validate(ValidCustomer() with { PostalCode = postalCode });

        Assert.Contains("PostalCode", errors.ToDictionary().Keys);
    }

    [Theory]
    [InlineData("anna")]
    [InlineData("anna@")]
    [InlineData("anna@example")]
    public void Email_must_look_like_an_address(string email)
    {
        var errors = _validator.Validate(ValidCustomer() with { Email = email });

        Assert.Contains("Email", errors.ToDictionary().Keys);
    }

    [Fact]
    public void Optional_fields_may_be_empty()
    {
        var errors = _validator.Validate(new CustomerInput("Anna", "Albers", null, null, null, null, null));

        Assert.True(errors.IsValid);
    }
}
