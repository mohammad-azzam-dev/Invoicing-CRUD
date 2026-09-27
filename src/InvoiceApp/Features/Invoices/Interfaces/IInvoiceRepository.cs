using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;

namespace InvoiceApp.Features.Invoices.Interfaces;

public interface IInvoiceRepository
{
    Task<int> UpsertInvoiceAsync(
        Invoice invoice,
        ICollection<LineItem> existingLineItems,
        IReadOnlyList<LineItemFormDto> newLineItems,
        CancellationToken ct = default
    );

    Task<PagedResult<InvoiceListItemDto>> GetPagedAsync(
        InvoiceQuery query,
        CancellationToken ct = default
    );
    Task<Invoice?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(Invoice? Invoice, int ItemCount)> GetWithLineItemCountAsync(
        int id,
        CancellationToken ct = default
    );
    Task<Invoice?> GetWithLineItemsAsync(int id, CancellationToken ct = default);
    Task<Invoice?> GetForEditAsync(int id, CancellationToken ct = default);
    Task<int> AddAsync(Invoice invoice, CancellationToken ct = default);
    Task UpdateAsync(Invoice invoice, CancellationToken ct = default);
    Task DeleteAsync(Invoice invoice, CancellationToken ct = default);
    Task<LineItem?> GetLineItemAsync(int invoiceId, int lineItemId, CancellationToken ct = default);
    Task AddLineItemAsync(LineItem lineItem, CancellationToken ct = default);
    Task UpdateLineItemAsync(LineItem lineItem, CancellationToken ct = default);
    Task RemoveLineItemAsync(LineItem lineItem, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerLookupDto>> GetCustomerLookupsAsync(CancellationToken ct = default);
    Task<InvoiceStatsDto> GetStatsAsync(CancellationToken ct = default);
}
