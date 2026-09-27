using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Interfaces;

namespace InvoiceApp.Tests.TestSupport;

public sealed class FakeInvoiceQueryService : IInvoiceQueryService
{
    private readonly List<InvoiceListItemDto> _invoices = [];
    private InvoiceDetailsDto? _detailsToReturn;
    private IReadOnlyList<CustomerLookupDto> _customers = [];

    public void SetInvoices(IEnumerable<InvoiceListItemDto> invoices)
    {
        _invoices.Clear();
        _invoices.AddRange(invoices);
    }

    public void SetDetails(InvoiceDetailsDto? details)
    {
        _detailsToReturn = details;
    }

    public void SetCustomers(IReadOnlyList<CustomerLookupDto> customers)
    {
        _customers = customers;
    }

    public Task<PagedResult<InvoiceListItemDto>> GetPagedAsync(
        InvoiceQuery query,
        CancellationToken ct = default
    )
    {
        IEnumerable<InvoiceListItemDto> filtered = _invoices.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string search = query.Search.Trim().ToLowerInvariant();
            filtered = filtered.Where(i =>
                i.CustomerName.ToLowerInvariant().Contains(search)
                || i.Number.ToLowerInvariant().Contains(search)
            );
        }

        if (query.Status.HasValue)
        {
            filtered = filtered.Where(i => i.Status == query.Status.Value);
        }

        int totalCount = filtered.Count();

        int skip = (query.Page - 1) * query.PageSize;
        List<InvoiceListItemDto> items = filtered.Skip(skip).Take(query.PageSize).ToList();

        return Task.FromResult(new PagedResult<InvoiceListItemDto>(items, totalCount));
    }

    public Task<InvoiceDetailsDto?> GetDetailsAsync(int id, CancellationToken ct = default)
    {
        return Task.FromResult(_detailsToReturn);
    }

    public Task<IReadOnlyList<CustomerLookupDto>> GetCustomersForDropdownAsync(
        CancellationToken ct = default
    )
    {
        return Task.FromResult(_customers);
    }

    public Task<InvoiceStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        int draft = _invoices.Count(i => i.Status == InvoiceStatus.Draft);
        int sent = _invoices.Count(i => i.Status == InvoiceStatus.Sent);
        int paid = _invoices.Count(i => i.Status == InvoiceStatus.Paid);
        int cancelled = _invoices.Count(i => i.Status == InvoiceStatus.Cancelled);
        int total = draft + sent + paid + cancelled;
        return Task.FromResult(new InvoiceStatsDto(total, draft, sent, paid, cancelled));
    }
}
