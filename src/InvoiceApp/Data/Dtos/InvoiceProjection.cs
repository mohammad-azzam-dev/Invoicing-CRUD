using InvoiceApp.Domain;

namespace InvoiceApp.Data.Dtos;

internal sealed record InvoiceProjection(
    int Id,
    string CustomerName,
    DateOnly IssueDate,
    DateOnly DueDate,
    InvoiceStatus Status,
    int ItemCount,
    decimal Total,
    bool IsOverdue);
