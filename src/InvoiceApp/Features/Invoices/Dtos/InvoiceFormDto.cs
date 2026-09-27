namespace InvoiceApp.Features.Invoices.Dtos;

public sealed record InvoiceFormDto(
    int CustomerId,
    DateOnly IssueDate,
    DateOnly DueDate,
    decimal TaxRate
);
