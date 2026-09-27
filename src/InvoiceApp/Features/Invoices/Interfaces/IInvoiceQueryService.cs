using InvoiceApp.Features.Invoices.Dtos;

namespace InvoiceApp.Features.Invoices.Interfaces;

public interface IInvoiceQueryService
{
    Task<PagedResult<InvoiceListItemDto>> GetPagedAsync(
        InvoiceQuery query,
        CancellationToken ct = default
    );
    Task<InvoiceDetailsDto?> GetDetailsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerLookupDto>> GetCustomersForDropdownAsync(
        CancellationToken ct = default
    );
    Task<InvoiceStatsDto> GetStatsAsync(CancellationToken ct = default);
}
