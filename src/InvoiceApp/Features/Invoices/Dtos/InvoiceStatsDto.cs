namespace InvoiceApp.Features.Invoices.Dtos;

public sealed record InvoiceStatsDto(int Total, int Draft, int Sent, int Paid, int Cancelled);
