namespace InvoiceApp.Features.Invoices.Dtos;

public sealed record LineItemFormDto(
    int Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent
);
