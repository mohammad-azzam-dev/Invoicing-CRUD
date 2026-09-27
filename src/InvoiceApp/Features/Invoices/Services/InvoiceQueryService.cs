using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Interfaces;

namespace InvoiceApp.Features.Invoices.Services;

public sealed class InvoiceQueryService(
    IInvoiceRepository repository,
    ILogger<InvoiceQueryService> logger
) : IInvoiceQueryService
{
    public async Task<PagedResult<InvoiceListItemDto>> GetPagedAsync(
        InvoiceQuery query,
        CancellationToken ct = default
    )
    {
        logger.LogDebug(
            "Getting paged invoices: Page={Page}, PageSize={PageSize}, Search={Search}, Status={Status}",
            query.Page,
            query.PageSize,
            query.Search,
            query.Status
        );
        return await repository.GetPagedAsync(query, ct);
    }

    public async Task<InvoiceDetailsDto?> GetDetailsAsync(int id, CancellationToken ct = default)
    {
        Invoice? invoice = await repository.GetWithLineItemsAsync(id, ct);

        if (invoice is null)
        {
            logger.LogDebug("Invoice not found: {InvoiceId}", id);
            return null;
        }

        return invoice.ToDetailsDto(invoice.LineItems);
    }

    public async Task<IReadOnlyList<CustomerLookupDto>> GetCustomersForDropdownAsync(
        CancellationToken ct = default
    )
    {
        return await repository.GetCustomerLookupsAsync(ct);
    }

    public async Task<InvoiceStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        return await repository.GetStatsAsync(ct);
    }
}
