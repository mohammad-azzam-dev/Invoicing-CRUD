using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;

namespace InvoiceApp.Features.Invoices.Interfaces;

public interface IInvoiceRepository
{
    Task<PagedResult<InvoiceListItemDto>> GetPagedAsync(InvoiceQuery query, CancellationToken ct = default);
    Task<Invoice?> GetByIdAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(Invoice invoice, CancellationToken ct = default);
}
