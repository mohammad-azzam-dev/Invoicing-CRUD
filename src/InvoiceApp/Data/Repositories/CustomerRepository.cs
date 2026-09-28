using InvoiceApp.Data.Dtos;
using InvoiceApp.Domain;
using InvoiceApp.Features.Customers;
using InvoiceApp.Features.Customers.Dtos;
using InvoiceApp.Features.Customers.Interfaces;
using InvoiceApp.Features.Invoices;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Data.Repositories;

public sealed class CustomerRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ICustomerRepository
{
    public async Task<PagedResult<CustomerListItemDto>> GetPagedAsync(
        CustomerQuery query,
        CancellationToken ct = default
    )
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);

        IQueryable<Customer> baseQuery = db.Customers.AsNoTracking();
        IQueryable<Customer> filtered = ApplyFilters(baseQuery, query);
        int totalCount = await filtered.CountAsync(ct);
        IOrderedQueryable<Customer> ordered = ApplySorting(
            filtered,
            query.SortBy,
            query.Descending
        );

        int skip = (query.Page - 1) * query.PageSize;
        List<CustomerProjection> items = await GetCustomerProjection(ordered)
            .Skip(skip)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<CustomerListItemDto>(items.Select(MapToDto).ToList(), totalCount);
    }

    private static IQueryable<CustomerProjection> GetCustomerProjection(IQueryable<Customer> query)
    {
        return query.Select(c => new CustomerProjection(
            c.Id,
            c.CompanyName != null ? c.CompanyName : c.Name,
            c.Name,
            c.CompanyName,
            c.Email,
            c.Phone,
            c.Invoices.Count
        ));
    }

    private static CustomerListItemDto MapToDto(CustomerProjection p)
    {
        return new CustomerListItemDto(
            p.Id,
            p.DisplayName,
            p.Name,
            p.CompanyName,
            p.Email,
            p.Phone,
            p.InvoiceCount
        );
    }

    private static IOrderedQueryable<Customer> ApplySorting(
        IQueryable<Customer> query,
        CustomerSortField sortBy,
        bool descending
    )
    {
        IOrderedQueryable<Customer> ordered = (sortBy, descending) switch
        {
            (CustomerSortField.DisplayName, true) => query.OrderByDescending(c =>
                c.CompanyName ?? c.Name
            ),
            (CustomerSortField.DisplayName, false) => query.OrderBy(c => c.CompanyName ?? c.Name),
            (CustomerSortField.Name, true) => query.OrderByDescending(c => c.Name),
            (CustomerSortField.Name, false) => query.OrderBy(c => c.Name),
            (CustomerSortField.Email, true) => query.OrderByDescending(c => c.Email),
            (CustomerSortField.Email, false) => query.OrderBy(c => c.Email),
            (CustomerSortField.Phone, true) => query.OrderByDescending(c => c.Phone),
            (CustomerSortField.Phone, false) => query.OrderBy(c => c.Phone),
            (CustomerSortField.InvoiceCount, true) => query.OrderByDescending(c =>
                c.Invoices.Count
            ),
            (CustomerSortField.InvoiceCount, false) => query.OrderBy(c => c.Invoices.Count),
            _ => query.OrderBy(c => c.CompanyName ?? c.Name),
        };

        return ordered.ThenBy(c => c.Id);
    }

    private static IQueryable<Customer> ApplyFilters(
        IQueryable<Customer> query,
        CustomerQuery filters
    )
    {
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            string searchLower = filters.Search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(searchLower)
                || (c.CompanyName != null && c.CompanyName.ToLower().Contains(searchLower))
                || c.Email.ToLower().Contains(searchLower)
                || c.Phone.ToLower().Contains(searchLower)
            );
        }

        return query;
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Customers.FindAsync([id], ct);
    }

    public async Task<CustomerWithInvoiceCount?> GetByIdWithInvoiceCountAsync(
        int id,
        CancellationToken ct = default
    )
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);

        return await db
            .Customers.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CustomerWithInvoiceCount(c, c.Invoices.Count))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> EmailExistsAsync(
        string email,
        int? excludeId = null,
        CancellationToken ct = default
    )
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        string emailLower = email.Trim().ToLower();

        IQueryable<Customer> query = db.Customers.Where(c => c.Email.ToLower() == emailLower);

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(ct);
    }

    public async Task<int> AddAsync(Customer customer, CancellationToken ct = default)
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return customer.Id;
    }

    public async Task UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        db.Customers.Attach(customer);
        db.Entry(customer).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Customer customer, CancellationToken ct = default)
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        db.Customers.Attach(customer);
        db.Customers.Remove(customer);
        await db.SaveChangesAsync(ct);
    }
}
