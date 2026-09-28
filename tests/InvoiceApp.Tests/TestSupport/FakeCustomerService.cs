using InvoiceApp.Domain;
using InvoiceApp.Features.Customers;
using InvoiceApp.Features.Customers.Dtos;
using InvoiceApp.Features.Customers.Interfaces;
using InvoiceApp.Features.Invoices;

namespace InvoiceApp.Tests.TestSupport;

public sealed class FakeCustomerService : ICustomerService
{
    private readonly List<CustomerListItemDto> _customers = [];
    private readonly Dictionary<int, CustomerDetailsDto> _details = new();
    private Result<int>? _createResult;
    private Result? _updateResult;
    private Result? _deleteResult;

    public void SetCustomers(IEnumerable<CustomerListItemDto> customers)
    {
        _customers.Clear();
        _customers.AddRange(customers);
    }

    public void SetDetails(CustomerDetailsDto details)
    {
        _details[details.Id] = details;
    }

    public void SetCreateResult(Result<int> result)
    {
        _createResult = result;
    }

    public void SetUpdateResult(Result result)
    {
        _updateResult = result;
    }

    public void SetDeleteResult(Result result)
    {
        _deleteResult = result;
    }

    public Task<PagedResult<CustomerListItemDto>> GetPagedAsync(
        CustomerQuery query,
        CancellationToken ct = default
    )
    {
        IEnumerable<CustomerListItemDto> filtered = _customers;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string search = query.Search.Trim().ToLower();
            filtered = filtered.Where(c =>
                c.Name.ToLower().Contains(search)
                || (c.CompanyName?.ToLower().Contains(search) ?? false)
                || c.Email.ToLower().Contains(search)
                || c.Phone.ToLower().Contains(search)
            );
        }

        int totalCount = filtered.Count();
        int skip = (query.Page - 1) * query.PageSize;
        List<CustomerListItemDto> items = filtered.Skip(skip).Take(query.PageSize).ToList();

        return Task.FromResult(new PagedResult<CustomerListItemDto>(items, totalCount));
    }

    public Task<CustomerDetailsDto?> GetDetailsAsync(int id, CancellationToken ct = default)
    {
        _details.TryGetValue(id, out CustomerDetailsDto? details);
        return Task.FromResult(details);
    }

    public Task<Result<int>> CreateAsync(CustomerFormDto form, CancellationToken ct = default)
    {
        return Task.FromResult(_createResult ?? Result<int>.Success(1));
    }

    public Task<Result> UpdateAsync(int id, CustomerFormDto form, CancellationToken ct = default)
    {
        return Task.FromResult(_updateResult ?? Result.Success());
    }

    public Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        return Task.FromResult(_deleteResult ?? Result.Success());
    }
}
