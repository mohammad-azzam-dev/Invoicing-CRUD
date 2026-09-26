using InvoiceApp.Domain;

namespace InvoiceApp.Features.Invoices.Dtos;

public sealed record InvoiceListItemDto(
    int Id,
    string Number,
    string CustomerName,
    DateOnly IssueDate,
    DateOnly DueDate,
    InvoiceStatus Status,
    bool IsOverdue,
    int ItemCount,
    decimal Total);
