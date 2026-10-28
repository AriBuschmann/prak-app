namespace PrakApp.Api.Customers;

public sealed record CustomerListItem(
    int Id,
    string CustomerNumber,
    string FirstName,
    string LastName,
    string? City,
    int ContractCount,
    decimal TotalPaid);

/// <summary>Möglicher doppelter Kunde (gleicher Name und gleiche PLZ), siehe GET /api/customers/duplicates.</summary>
public sealed record CustomerDuplicate(
    int Id,
    string CustomerNumber,
    string FirstName,
    string LastName,
    string? PostalCode,
    string? City);

public sealed record Customer(
    int Id,
    string CustomerNumber,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? Street,
    string? PostalCode,
    string? City,
    DateTime CreatedAt,
    string CreatedBy);

/// <summary>Eingabedaten beim Anlegen und Bearbeiten eines Kunden.</summary>
public sealed record CustomerInput(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? Street,
    string? PostalCode,
    string? City);
