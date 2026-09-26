using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;

namespace InvoiceApp.Features.Invoices.Interfaces;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceListItemDto>> GetPagedAsync(InvoiceQuery query, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}
