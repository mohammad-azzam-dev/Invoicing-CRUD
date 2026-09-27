using InvoiceApp.Domain;

namespace InvoiceApp.Features.Invoices;

public sealed record InvoiceQuery(
    string? Search,
    InvoiceStatus? Status,
    InvoiceSortField SortBy,
    bool Descending,
    int Page,
    int PageSize
)
{
    public static InvoiceQuery Default =>
        new(
            Search: null,
            Status: null,
            SortBy: InvoiceSortField.IssueDate,
            Descending: true,
            Page: 1,
            PageSize: 10
        );
}
