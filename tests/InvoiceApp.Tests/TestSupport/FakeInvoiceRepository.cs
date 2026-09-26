using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Interfaces;

namespace InvoiceApp.Tests.TestSupport;

public sealed class FakeInvoiceRepository : IInvoiceRepository
{
    private readonly List<InvoiceListItemDto> _invoices = [];
    private readonly Dictionary<int, Invoice> _invoiceEntities = new();

    public void SetInvoices(IEnumerable<InvoiceListItemDto> invoices)
    {
        _invoices.Clear();
        _invoices.AddRange(invoices);
    }

    public void SetInvoiceEntity(Invoice invoice)
    {
        _invoiceEntities[invoice.Id] = invoice;
    }

    public bool WasDeleted(int id) => _deletedIds.Contains(id);

    private readonly HashSet<int> _deletedIds = [];

    public Task<Invoice?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        _invoiceEntities.TryGetValue(id, out var invoice);
        return Task.FromResult(invoice);
    }

    public Task DeleteAsync(Invoice invoice, CancellationToken ct = default)
    {
        _deletedIds.Add(invoice.Id);
        _invoiceEntities.Remove(invoice.Id);
        return Task.CompletedTask;
    }

    public Task<PagedResult<InvoiceListItemDto>> GetPagedAsync(InvoiceQuery query, CancellationToken ct = default)
    {
        var filtered = _invoices.AsEnumerable();

        // Apply search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            filtered = filtered.Where(i =>
                i.CustomerName.ToLowerInvariant().Contains(search) ||
                i.Number.ToLowerInvariant().Contains(search));
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
        bool descending)
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
            _ => items.OrderByDescending(i => i.IssueDate)
        };

        return ordered.ThenBy(i => i.Id);
    }
}
