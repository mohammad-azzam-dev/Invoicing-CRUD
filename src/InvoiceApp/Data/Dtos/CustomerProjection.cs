namespace InvoiceApp.Data.Dtos;

internal sealed record CustomerProjection(
    int Id,
    string DisplayName,
    string Name,
    string? CompanyName,
    string Email,
    string Phone,
    int InvoiceCount
);
