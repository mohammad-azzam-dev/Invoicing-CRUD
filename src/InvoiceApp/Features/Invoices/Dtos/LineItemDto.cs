namespace InvoiceApp.Features.Invoices.Dtos;

public sealed record LineItemDto(
    int Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal LineTotal
);
