using InvoiceApp.Domain;
using InvoiceApp.Features.Customers;
using InvoiceApp.Features.Customers.Dtos;
using InvoiceApp.Features.Customers.Interfaces;
using InvoiceApp.Features.Invoices;

namespace InvoiceApp.Tests.TestSupport;

public sealed class FakeCustomerRepository : ICustomerRepository
{
    private readonly List<CustomerListItemDto> _customers = [];
    private readonly Dictionary<int, Customer> _customerEntities = new();
    private readonly Dictionary<int, int> _invoiceCounts = new();
    private readonly HashSet<string> _emails = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<int> _deletedIds = [];
    private readonly HashSet<int> _updatedIds = [];
    private int _nextId = 1;

    public void SetCustomers(IEnumerable<CustomerListItemDto> customers)
    {
        _customers.Clear();
        _customers.AddRange(customers);
    }

    public void SetCustomerEntity(Customer customer, int invoiceCount = 0)
    {
        _customerEntities[customer.Id] = customer;
        _invoiceCounts[customer.Id] = invoiceCount;
        _emails.Add(customer.Email);
    }

    public void AddEmail(string email)
    {
        _emails.Add(email);
    }

    public bool WasDeleted(int id) => _deletedIds.Contains(id);

    public bool WasUpdated(int id) => _updatedIds.Contains(id);

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

        filtered = ApplySorting(filtered, query.SortBy, query.Descending);

        int skip = (query.Page - 1) * query.PageSize;
        List<CustomerListItemDto> items = filtered.Skip(skip).Take(query.PageSize).ToList();

        return Task.FromResult(new PagedResult<CustomerListItemDto>(items, totalCount));
    }

    private static IEnumerable<CustomerListItemDto> ApplySorting(
        IEnumerable<CustomerListItemDto> items,
        CustomerSortField sortBy,
        bool descending
    )
    {
        IOrderedEnumerable<CustomerListItemDto> ordered = (sortBy, descending) switch
        {
            (CustomerSortField.DisplayName, true) => items.OrderByDescending(c => c.DisplayName),
            (CustomerSortField.DisplayName, false) => items.OrderBy(c => c.DisplayName),
            (CustomerSortField.Name, true) => items.OrderByDescending(c => c.Name),
            (CustomerSortField.Name, false) => items.OrderBy(c => c.Name),
            (CustomerSortField.Email, true) => items.OrderByDescending(c => c.Email),
            (CustomerSortField.Email, false) => items.OrderBy(c => c.Email),
            (CustomerSortField.Phone, true) => items.OrderByDescending(c => c.Phone),
            (CustomerSortField.Phone, false) => items.OrderBy(c => c.Phone),
            (CustomerSortField.InvoiceCount, true) => items.OrderByDescending(c => c.InvoiceCount),
            (CustomerSortField.InvoiceCount, false) => items.OrderBy(c => c.InvoiceCount),
            _ => items.OrderBy(c => c.DisplayName),
        };

        return ordered.ThenBy(c => c.Id);
    }

    public Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        _customerEntities.TryGetValue(id, out Customer? customer);
        return Task.FromResult(customer);
    }

    public Task<CustomerWithInvoiceCount?> GetByIdWithInvoiceCountAsync(
        int id,
        CancellationToken ct = default
    )
    {
        if (_customerEntities.TryGetValue(id, out Customer? customer))
        {
            int invoiceCount = _invoiceCounts.GetValueOrDefault(id, 0);
            return Task.FromResult<CustomerWithInvoiceCount?>(
                new CustomerWithInvoiceCount(customer, invoiceCount)
            );
        }
        return Task.FromResult<CustomerWithInvoiceCount?>(null);
    }

    public Task<bool> EmailExistsAsync(
        string email,
        int? excludeId = null,
        CancellationToken ct = default
    )
    {
        string emailLower = email.Trim().ToLower();

        if (excludeId.HasValue)
        {
            Customer? excluded = _customerEntities.GetValueOrDefault(excludeId.Value);
            if (excluded is not null && excluded.Email.ToLower() == emailLower)
            {
                return Task.FromResult(false);
            }
        }

        bool exists = _emails.Any(e => e.ToLower() == emailLower);
        return Task.FromResult(exists);
    }

    public Task<int> AddAsync(Customer customer, CancellationToken ct = default)
    {
        // Simulate ID assignment
        int id = _nextId++;
        _customerEntities[id] = customer;
        _emails.Add(customer.Email);
        return Task.FromResult(id);
    }

    public Task UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        _customerEntities[customer.Id] = customer;
        _updatedIds.Add(customer.Id);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Customer customer, CancellationToken ct = default)
    {
        _deletedIds.Add(customer.Id);
        _customerEntities.Remove(customer.Id);
        _emails.Remove(customer.Email);
        return Task.CompletedTask;
    }
}
