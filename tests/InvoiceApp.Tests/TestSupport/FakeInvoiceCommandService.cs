using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Interfaces;

namespace InvoiceApp.Tests.TestSupport;

public sealed class FakeInvoiceCommandService : IInvoiceCommandService
{
    private readonly HashSet<int> _deletedIds = [];
    private readonly Dictionary<int, InvoiceStatus> _statusChanges = new();
    private readonly List<(int Id, InvoiceFormDto Form, IReadOnlyList<LineItemFormDto> LineItems)> _savedInvoices = [];
    private Result<int>? _saveResult;
    private Result? _deleteResult;
    private Result? _changeStatusResult;
    private int _nextId = 1;

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
        return Task.FromResult(Result.Success());
    }
}
