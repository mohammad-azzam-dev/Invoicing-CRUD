using System.Linq.Expressions;
using InvoiceApp.Data.Dtos;
using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Data.Repositories;

public sealed class InvoiceRepository(
    IDbContextFactory<AppDbContext> dbFactory,
    TimeProvider timeProvider
) : IInvoiceRepository
{
    private static readonly Expression<Func<Invoice, double>> TotalExpression = i =>
        i.LineItems.Sum(l =>
            (double)(l.Quantity * l.UnitPrice)
            - (double)(l.Quantity * l.UnitPrice * l.DiscountPercent / 100m)
        ) * (1 + (double)i.TaxRate / 100.0);

    public async Task<int> UpsertInvoiceAsync(
        Invoice invoice,
        ICollection<LineItem> existingLineItems,
        IReadOnlyList<LineItemFormDto> newLineItems,
        CancellationToken ct = default
    )
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        try
        {
            SaveInvoiceHeader(db, invoice);
            await db.SaveChangesAsync(ct);

            SyncLineItems(db, invoice.Id, existingLineItems, newLineItems);
            await db.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
            return invoice.Id;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static void SaveInvoiceHeader(AppDbContext db, Invoice invoice)
    {
        if (invoice.Id == 0)
        {
            db.Invoices.Add(invoice);
        }
        else
        {
            db.Invoices.Attach(invoice);
            db.Entry(invoice).State = EntityState.Modified;
        }
    }

    private static void SyncLineItems(
        AppDbContext db,
        int invoiceId,
        ICollection<LineItem> existingItems,
        IReadOnlyList<LineItemFormDto> newItems
    )
    {
        HashSet<int> existingIds = existingItems.Select(i => i.Id).ToHashSet();
        HashSet<int> newIds = newItems.Where(i => i.Id > 0).Select(i => i.Id).ToHashSet();

        // Remove deleted items
        foreach (LineItem item in existingItems.Where(i => !newIds.Contains(i.Id)))
        {
            db.LineItems.Remove(item);
        }

        // Add or update items
        foreach (LineItemFormDto itemDto in newItems)
        {
            if (itemDto.Id == 0)
            {
                LineItem newItem = LineItem.Create(
                    invoiceId,
                    itemDto.Description,
                    itemDto.Quantity,
                    itemDto.UnitPrice,
                    itemDto.DiscountPercent
                );
                db.LineItems.Add(newItem);
            }
            else if (existingIds.Contains(itemDto.Id))
            {
                LineItem? existingItem = existingItems.FirstOrDefault(i => i.Id == itemDto.Id);
                existingItem?.Update(
                    itemDto.Description,
                    itemDto.Quantity,
                    itemDto.UnitPrice,
                    itemDto.DiscountPercent
                );
            }
        }
    }

    public async Task<PagedResult<InvoiceListItemDto>> GetPagedAsync(
        InvoiceQuery query,
        CancellationToken ct = default
    )
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().DateTime);

        IQueryable<Invoice> baseQuery = db.Invoices.AsNoTracking().Include(i => i.Customer);
        IQueryable<Invoice> filtered = ApplyFilters(baseQuery, query);
        int totalCount = await filtered.CountAsync(ct);
        IOrderedQueryable<Invoice> ordered = ApplySorting(filtered, query.SortBy, query.Descending);

        int skip = (query.Page - 1) * query.PageSize;
        List<InvoiceProjection> items = await GetInvoiceProjection(ordered, today)
            .Skip(skip)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<InvoiceListItemDto>(items.Select(MapToDto).ToList(), totalCount);
    }

    private static IQueryable<InvoiceProjection> GetInvoiceProjection(
        IQueryable<Invoice> query,
        DateOnly today
    )
    {
        return query
            .Select(i => new
            {
                i.Id,
                CustomerName = i.Customer.CompanyName ?? i.Customer.Name,
                i.IssueDate,
                i.DueDate,
                i.Status,
                i.TaxRate,
                ItemCount = i.LineItems.Count,
                Subtotal = i.LineItems.Sum(l =>
                    (double)(l.Quantity * l.UnitPrice)
                    - (double)(l.Quantity * l.UnitPrice * l.DiscountPercent / 100m)
                ),
                IsOverdue = i.Status == InvoiceStatus.Sent && i.DueDate < today,
            })
            .Select(i => new InvoiceProjection(
                i.Id,
                i.CustomerName,
                i.IssueDate,
                i.DueDate,
                i.Status,
                i.ItemCount,
                (decimal)(i.Subtotal * (1 + (double)i.TaxRate / 100.0)),
                i.IsOverdue
            ));
    }

    private static InvoiceListItemDto MapToDto(InvoiceProjection p)
    {
        return new InvoiceListItemDto(
            p.Id,
            InvoiceNumber.Format(p.Id),
            p.CustomerName,
            p.IssueDate,
            p.DueDate,
            p.Status,
            p.IsOverdue,
            p.ItemCount,
            p.Total,
            InvoiceStatusRules.AllowedNext(p.Status)
        );
    }

    private static IOrderedQueryable<Invoice> ApplySorting(
        IQueryable<Invoice> query,
        InvoiceSortField sortBy,
        bool descending
    )
    {
        IOrderedQueryable<Invoice> ordered = (sortBy, descending) switch
        {
            (InvoiceSortField.Number, true) => query.OrderByDescending(i => i.Id),
            (InvoiceSortField.Number, false) => query.OrderBy(i => i.Id),
            (InvoiceSortField.CustomerName, true) => query.OrderByDescending(i =>
                i.Customer.CompanyName ?? i.Customer.Name
            ),
            (InvoiceSortField.CustomerName, false) => query.OrderBy(i =>
                i.Customer.CompanyName ?? i.Customer.Name
            ),
            (InvoiceSortField.IssueDate, true) => query.OrderByDescending(i => i.IssueDate),
            (InvoiceSortField.IssueDate, false) => query.OrderBy(i => i.IssueDate),
            (InvoiceSortField.DueDate, true) => query.OrderByDescending(i => i.DueDate),
            (InvoiceSortField.DueDate, false) => query.OrderBy(i => i.DueDate),
            (InvoiceSortField.Status, true) => query.OrderByDescending(i => i.Status),
            (InvoiceSortField.Status, false) => query.OrderBy(i => i.Status),
            (InvoiceSortField.ItemCount, true) => query.OrderByDescending(i => i.LineItems.Count),
            (InvoiceSortField.ItemCount, false) => query.OrderBy(i => i.LineItems.Count),
            (InvoiceSortField.Total, true) => query.OrderByDescending(TotalExpression),
            (InvoiceSortField.Total, false) => query.OrderBy(TotalExpression),
            _ => query.OrderByDescending(i => i.IssueDate),
        };

        return ordered.ThenBy(i => i.Id);
    }

    public async Task<Invoice?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Invoices.FindAsync([id], ct);
    }

    public async Task<(Invoice? Invoice, int ItemCount)> GetWithLineItemCountAsync(
        int id,
        CancellationToken ct = default
    )
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);

        var result = await db
            .Invoices.Where(i => i.Id == id)
            .Select(i => new { Invoice = i, ItemCount = i.LineItems.Count })
            .FirstOrDefaultAsync(ct);

        return result is null ? (null, 0) : (result.Invoice, result.ItemCount);
    }

    public async Task UpdateAsync(Invoice invoice, CancellationToken ct = default)
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        db.Invoices.Attach(invoice);
        db.Entry(invoice).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Invoice invoice, CancellationToken ct = default)
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        db.Invoices.Attach(invoice);
        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Invoice?> GetWithLineItemsAsync(int id, CancellationToken ct = default)
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        return await db
            .Invoices.Include(i => i.Customer)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<Invoice?> GetForEditAsync(int id, CancellationToken ct = default)
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        return await db
            .Invoices.Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<int> AddAsync(Invoice invoice, CancellationToken ct = default)
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);
        return invoice.Id;
    }

    public async Task<LineItem?> GetLineItemAsync(
        int invoiceId,
        int lineItemId,
        CancellationToken ct = default
    )
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        return await db.LineItems.FirstOrDefaultAsync(
            l => l.InvoiceId == invoiceId && l.Id == lineItemId,
            ct
        );
    }

    public async Task AddLineItemAsync(LineItem lineItem, CancellationToken ct = default)
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        db.LineItems.Add(lineItem);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateLineItemAsync(LineItem lineItem, CancellationToken ct = default)
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        db.LineItems.Attach(lineItem);
        db.Entry(lineItem).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveLineItemAsync(LineItem lineItem, CancellationToken ct = default)
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        db.LineItems.Attach(lineItem);
        db.LineItems.Remove(lineItem);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CustomerLookupDto>> GetCustomerLookupsAsync(
        CancellationToken ct = default
    )
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        return await db
            .Customers.AsNoTracking()
            .OrderBy(c => c.CompanyName ?? c.Name)
            .ThenBy(c => c.Id)
            .Select(c => new CustomerLookupDto(c.Id, c.CompanyName ?? c.Name))
            .ToListAsync(ct);
    }

    public async Task<InvoiceStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);

        int draft = await db.Invoices.CountAsync(i => i.Status == InvoiceStatus.Draft, ct);
        int sent = await db.Invoices.CountAsync(i => i.Status == InvoiceStatus.Sent, ct);
        int paid = await db.Invoices.CountAsync(i => i.Status == InvoiceStatus.Paid, ct);
        int cancelled = await db.Invoices.CountAsync(i => i.Status == InvoiceStatus.Cancelled, ct);
        int total = draft + sent + paid + cancelled;

        return new InvoiceStatsDto(total, draft, sent, paid, cancelled);
    }

    private static IQueryable<Invoice> ApplyFilters(IQueryable<Invoice> query, InvoiceQuery filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            string searchLower = filters.Search.Trim().ToLowerInvariant();
            int? invoiceId = InvoiceNumber.TryParse(filters.Search);

            query = query.Where(i =>
                i.Customer.Name.ToLower().Contains(searchLower)
                || (
                    i.Customer.CompanyName != null
                    && i.Customer.CompanyName.ToLower().Contains(searchLower)
                )
                || (invoiceId != null && i.Id == invoiceId)
            );
        }

        if (filters.Status.HasValue)
        {
            query = query.Where(i => i.Status == filters.Status.Value);
        }

        return query;
    }
}
