namespace InvoiceApp.Features.Customers;

public sealed record CustomerQuery(
    string? Search,
    CustomerSortField SortBy,
    bool Descending,
    int Page,
    int PageSize
)
{
    public static CustomerQuery Default =>
        new(
            Search: null,
            SortBy: CustomerSortField.DisplayName,
            Descending: false,
            Page: 1,
            PageSize: 10
        );
}
