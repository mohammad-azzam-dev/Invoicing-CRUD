namespace InvoiceApp.Features.Customers.Dtos;

public sealed record CustomerListItemDto(
    int Id,
    string DisplayName,
    string Name,
    string? CompanyName,
    string Email,
    string Phone,
    int InvoiceCount
);
