using InvoiceApp.Domain;

namespace InvoiceApp.Features.Invoices.Dtos;

public sealed record InvoiceDetailsDto(
    int Id,
    string Number,
    int CustomerId,
    string CustomerName,
    DateOnly IssueDate,
    DateOnly DueDate,
    InvoiceStatus Status,
    decimal TaxRate,
    bool CanEdit,
    IReadOnlyList<LineItemDto> LineItems,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxAmount,
    decimal Total
);
