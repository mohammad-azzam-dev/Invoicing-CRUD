namespace InvoiceApp.Features.Invoices;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount);
