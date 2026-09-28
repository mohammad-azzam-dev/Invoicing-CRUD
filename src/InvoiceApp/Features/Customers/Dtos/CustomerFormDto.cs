namespace InvoiceApp.Features.Customers.Dtos;

public sealed record CustomerFormDto(
    int? Id,
    string Name,
    string Email,
    string Phone,
    string? CompanyName,
    string? Address
);
