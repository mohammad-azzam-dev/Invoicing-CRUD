using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;

namespace InvoiceApp.Features.Invoices.Interfaces;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceListItemDto>> GetPagedAsync(
        InvoiceQuery query,
        CancellationToken ct = default
    );
    Task<InvoiceDetailsDto?> GetDetailsAsync(int id, CancellationToken ct = default);
    Task<Result<int>> SaveAsync(
        int id,
        InvoiceFormDto form,
        IReadOnlyList<LineItemFormDto> lineItems,
        CancellationToken ct = default
    );
    Task<Result> ChangeStatusAsync(int id, InvoiceStatus newStatus, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerLookupDto>> GetCustomersForDropdownAsync(
        CancellationToken ct = default
    );
    Task<InvoiceStatsDto> GetStatsAsync(CancellationToken ct = default);
}
