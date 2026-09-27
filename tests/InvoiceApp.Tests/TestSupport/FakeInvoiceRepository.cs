using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Interfaces;

namespace InvoiceApp.Tests.TestSupport;

public sealed class FakeInvoiceRepository : IInvoiceRepository
{
    private readonly List<InvoiceListItemDto> _invoices = [];
    private readonly Dictionary<int, Invoice> _invoiceEntities = new();
    private readonly Dictionary<int, int> _lineItemCounts = new();
    private readonly HashSet<int> _deletedIds = [];
    private readonly HashSet<int> _updatedIds = [];

    public void SetInvoices(IEnumerable<InvoiceListItemDto> invoices)
    {
        _invoices.Clear();
        _invoices.AddRange(invoices);
    }

    public void SetInvoiceEntity(Invoice invoice, int lineItemCount = 0)
    {
        _invoiceEntities[invoice.Id] = invoice;
        _lineItemCounts[invoice.Id] = lineItemCount;
    }

    public bool WasDeleted(int id) => _deletedIds.Contains(id);

    public bool WasUpdated(int id) => _updatedIds.Contains(id);

    public Task<int> UpsertInvoiceAsync(
        Invoice invoice,
        ICollection<LineItem> existingLineItems,
        IReadOnlyList<LineItemFormDto> newLineItems,
        CancellationToken ct = default
    )
    {
        _invoiceEntities[invoice.Id] = invoice;
        _updatedIds.Add(invoice.Id);
        return Task.FromResult(invoice.Id);
    }

    public Task<Invoice?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        _invoiceEntities.TryGetValue(id, out Invoice? invoice);
        return Task.FromResult(invoice);
    }

    public Task<(Invoice? Invoice, int ItemCount)> GetWithLineItemCountAsync(
        int id,
        CancellationToken ct = default
    )
    {
        if (_invoiceEntities.TryGetValue(id, out Invoice? invoice))
        {
            int itemCount = _lineItemCounts.GetValueOrDefault(id, 0);
            return Task.FromResult<(Invoice?, int)>((invoice, itemCount));
        }
        return Task.FromResult<(Invoice?, int)>((null, 0));
    }

    public Task UpdateAsync(Invoice invoice, CancellationToken ct = default)
    {
        _invoiceEntities[invoice.Id] = invoice;
        _updatedIds.Add(invoice.Id);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Invoice invoice, CancellationToken ct = default)
    {
        _deletedIds.Add(invoice.Id);
        _invoiceEntities.Remove(invoice.Id);
        return Task.CompletedTask;
    }

    public Task<Invoice?> GetWithLineItemsAsync(int id, CancellationToken ct = default)
    {
        _invoiceEntities.TryGetValue(id, out Invoice? invoice);
        return Task.FromResult(invoice);
    }

    public Task<int> AddAsync(Invoice invoice, CancellationToken ct = default)
    {
        _invoiceEntities[invoice.Id] = invoice;
        return Task.FromResult(invoice.Id);
    }

    public Task<LineItem?> GetLineItemAsync(
        int invoiceId,
        int lineItemId,
        CancellationToken ct = default
    )
    {
        return Task.FromResult<LineItem?>(null);
    }

    public Task AddLineItemAsync(LineItem lineItem, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task UpdateLineItemAsync(LineItem lineItem, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task RemoveLineItemAsync(LineItem lineItem, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CustomerLookupDto>> GetCustomerLookupsAsync(
        CancellationToken ct = default
    )
    {
        return Task.FromResult<IReadOnlyList<CustomerLookupDto>>([]);
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

    public Task<PagedResult<InvoiceListItemDto>> GetPagedAsync(
        InvoiceQuery query,
        CancellationToken ct = default
    )
    {
        var filtered = _invoices.AsEnumerable();

        // Apply search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            filtered = filtered.Where(i =>
                i.CustomerName.ToLowerInvariant().Contains(search)
                || i.Number.ToLowerInvariant().Contains(search)
            );
        }

        // Apply status filter
        if (query.Status.HasValue)
        {
            filtered = filtered.Where(i => i.Status == query.Status.Value);
        }

        var totalCount = filtered.Count();

        // Apply sorting
        filtered = ApplySorting(filtered, query.SortBy, query.Descending);

        // Apply paging
        var skip = (query.Page - 1) * query.PageSize;
        var items = filtered.Skip(skip).Take(query.PageSize).ToList();

        return Task.FromResult(new PagedResult<InvoiceListItemDto>(items, totalCount));
    }

    private static IEnumerable<InvoiceListItemDto> ApplySorting(
        IEnumerable<InvoiceListItemDto> items,
        InvoiceSortField sortBy,
        bool descending
    )
    {
        var ordered = (sortBy, descending) switch
        {
            (InvoiceSortField.Number, true) => items.OrderByDescending(i => i.Id),
            (InvoiceSortField.Number, false) => items.OrderBy(i => i.Id),
            (InvoiceSortField.CustomerName, true) => items.OrderByDescending(i => i.CustomerName),
            (InvoiceSortField.CustomerName, false) => items.OrderBy(i => i.CustomerName),
            (InvoiceSortField.IssueDate, true) => items.OrderByDescending(i => i.IssueDate),
            (InvoiceSortField.IssueDate, false) => items.OrderBy(i => i.IssueDate),
            (InvoiceSortField.DueDate, true) => items.OrderByDescending(i => i.DueDate),
            (InvoiceSortField.DueDate, false) => items.OrderBy(i => i.DueDate),
            (InvoiceSortField.Status, true) => items.OrderByDescending(i => i.Status),
            (InvoiceSortField.Status, false) => items.OrderBy(i => i.Status),
            (InvoiceSortField.ItemCount, true) => items.OrderByDescending(i => i.ItemCount),
            (InvoiceSortField.ItemCount, false) => items.OrderBy(i => i.ItemCount),
            (InvoiceSortField.Total, true) => items.OrderByDescending(i => i.Total),
            (InvoiceSortField.Total, false) => items.OrderBy(i => i.Total),
            _ => items.OrderByDescending(i => i.IssueDate),
        };

        return ordered.ThenBy(i => i.Id);
    }
}
