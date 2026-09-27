using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Interfaces;

namespace InvoiceApp.Tests.TestSupport;

public sealed class FakeInvoiceService : IInvoiceService
{
    private readonly List<InvoiceListItemDto> _invoices = [];
    private readonly HashSet<int> _deletedIds = [];
    private readonly Dictionary<int, InvoiceStatus> _statusChanges = new();
    private readonly List<(int Id, InvoiceFormDto Form, IReadOnlyList<LineItemFormDto> LineItems)> _savedInvoices = [];
    private Result<int>? _saveResult;
    private Result? _deleteResult;
    private Result? _changeStatusResult;
    private int _nextId = 1;

    public void SetInvoices(IEnumerable<InvoiceListItemDto> invoices)
    {
        _invoices.Clear();
        _invoices.AddRange(invoices);
    }

    public void SetSaveResult(Result<int> result)
    {
        _saveResult = result;
    }

    public void SetDeleteResult(Result result)
    {
        _deleteResult = result;
    }

    public void SetChangeStatusResult(Result result)
    {
        _changeStatusResult = result;
    }

    public bool WasDeleted(int id) => _deletedIds.Contains(id);

    public (int Id, InvoiceFormDto Form, IReadOnlyList<LineItemFormDto> LineItems)? GetLastSavedInvoice()
    {
        return _savedInvoices.Count > 0 ? _savedInvoices[^1] : null;
    }

    public (int Id, InvoiceStatus Status)? GetLastStatusChange()
    {
        if (_statusChanges.Count == 0)
        {
            return null;
        }
        KeyValuePair<int, InvoiceStatus> last = _statusChanges.Last();
        return (last.Key, last.Value);
    }

    public Task<PagedResult<InvoiceListItemDto>> GetPagedAsync(
        InvoiceQuery query,
        CancellationToken ct = default
    )
    {
        var filtered = _invoices.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            filtered = filtered.Where(i =>
                i.CustomerName.ToLowerInvariant().Contains(search)
                || i.Number.ToLowerInvariant().Contains(search)
            );
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

    public Task<Result<int>> SaveAsync(
        int id,
        InvoiceFormDto form,
        IReadOnlyList<LineItemFormDto> lineItems,
        CancellationToken ct = default
    )
    {
        if (_saveResult is not null)
        {
            return Task.FromResult(_saveResult);
        }

        int resultId = id == 0 ? _nextId++ : id;
        _savedInvoices.Add((resultId, form, lineItems));
        return Task.FromResult(Result<int>.Success(resultId));
    }

    public Task<Result> ChangeStatusAsync(
        int id,
        InvoiceStatus newStatus,
        CancellationToken ct = default
    )
    {
        if (_changeStatusResult is not null)
        {
            return Task.FromResult(_changeStatusResult);
        }

        _statusChanges[id] = newStatus;
        return Task.FromResult(Result.Success());
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

    public Task<InvoiceDetailsDto?> GetDetailsAsync(int id, CancellationToken ct = default)
    {
        return Task.FromResult<InvoiceDetailsDto?>(null);
    }

    public Task<IReadOnlyList<CustomerLookupDto>> GetCustomersForDropdownAsync(
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
}
