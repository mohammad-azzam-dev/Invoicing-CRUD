using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Interfaces;

namespace InvoiceApp.Tests.TestSupport;

public sealed class FakeInvoiceService : IInvoiceService
{
    private readonly List<InvoiceListItemDto> _invoices = [];
    private readonly HashSet<int> _deletedIds = [];
    private Result? _deleteResult;

    public void SetInvoices(IEnumerable<InvoiceListItemDto> invoices)
    {
        _invoices.Clear();
        _invoices.AddRange(invoices);
    }

    public void SetDeleteResult(Result result)
    {
        _deleteResult = result;
    }

    public bool WasDeleted(int id) => _deletedIds.Contains(id);

    public Task<PagedResult<InvoiceListItemDto>> GetPagedAsync(InvoiceQuery query, CancellationToken ct = default)
    {
        var filtered = _invoices.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            filtered = filtered.Where(i =>
                i.CustomerName.ToLowerInvariant().Contains(search) ||
                i.Number.ToLowerInvariant().Contains(search));
        }

        if (query.Status.HasValue)
        {
            filtered = filtered.Where(i => i.Status == query.Status.Value);
        }

        var totalCount = filtered.Count();

        var skip = (query.Page - 1) * query.PageSize;
        var items = filtered.Skip(skip).Take(query.PageSize).ToList();

        return Task.FromResult(new PagedResult<InvoiceListItemDto>(items, totalCount));
    }

    public Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        if (_deleteResult is not null)
        {
            return Task.FromResult(_deleteResult);
        }

        _deletedIds.Add(id);
        _invoices.RemoveAll(i => i.Id == id);
        return Task.FromResult(Result.Success());
    }
}
