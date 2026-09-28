namespace InvoiceApp.Features.Customers.Dtos;

public sealed record CustomerDetailsDto(
    int Id,
    string Name,
    string? CompanyName,
    string? Address,
    string Phone,
    string Email,
    int InvoiceCount
);
