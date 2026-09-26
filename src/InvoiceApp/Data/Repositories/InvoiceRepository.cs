using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using InvoiceApp.Data.Dtos;
using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Interfaces;

namespace InvoiceApp.Data.Repositories;

public sealed class InvoiceRepository(
    IDbContextFactory<AppDbContext> dbFactory,
    TimeProvider timeProvider) : IInvoiceRepository
{
    private static readonly Expression<Func<Invoice, double>> TotalExpression = i =>
        i.LineItems.Sum(l =>
            (double)(l.Quantity * l.UnitPrice) -
            (double)(l.Quantity * l.UnitPrice * l.DiscountPercent / 100m)) *
        (1 + (double)i.TaxRate / 100.0);

    public async Task<PagedResult<InvoiceListItemDto>> GetPagedAsync(InvoiceQuery query, CancellationToken ct = default)
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().DateTime);

        IQueryable<Invoice> baseQuery = db.Invoices.AsNoTracking();
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

    private static IQueryable<InvoiceProjection> GetInvoiceProjection(IQueryable<Invoice> query, DateOnly today)
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
                    (double)(l.Quantity * l.UnitPrice) -
                    (double)(l.Quantity * l.UnitPrice * l.DiscountPercent / 100m)),
                IsOverdue = i.Status == InvoiceStatus.Sent && i.DueDate < today
            })
            .Select(i => new InvoiceProjection(
                i.Id,
                i.CustomerName,
                i.IssueDate,
                i.DueDate,
                i.Status,
                i.ItemCount,
                (decimal)(i.Subtotal * (1 + (double)i.TaxRate / 100.0)),
                i.IsOverdue));
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
            p.Total);
    }

    private static IOrderedQueryable<Invoice> ApplySorting(
        IQueryable<Invoice> query,
        InvoiceSortField sortBy,
        bool descending)
    {
        IOrderedQueryable<Invoice> ordered = (sortBy, descending) switch
        {
            (InvoiceSortField.Number, true) => query.OrderByDescending(i => i.Id),
            (InvoiceSortField.Number, false) => query.OrderBy(i => i.Id),
            (InvoiceSortField.CustomerName, true) => query.OrderByDescending(i => i.Customer.CompanyName ?? i.Customer.Name),
            (InvoiceSortField.CustomerName, false) => query.OrderBy(i => i.Customer.CompanyName ?? i.Customer.Name),
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
            _ => query.OrderByDescending(i => i.IssueDate)
        };

        return ordered.ThenBy(i => i.Id);
    }

    public async Task<Invoice?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Invoices.FindAsync([id], ct);
    }

    public async Task DeleteAsync(Invoice invoice, CancellationToken ct = default)
    {
        await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
        db.Invoices.Attach(invoice);
        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync(ct);
    }

    private static IQueryable<Invoice> ApplyFilters(IQueryable<Invoice> query, InvoiceQuery filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            string searchLower = filters.Search.Trim().ToLowerInvariant();
            int? invoiceId = InvoiceNumber.TryParse(filters.Search);

            query = query.Where(i =>
                i.Customer.Name.ToLower().Contains(searchLower) ||
                (i.Customer.CompanyName != null && i.Customer.CompanyName.ToLower().Contains(searchLower)) ||
                (invoiceId != null && i.Id == invoiceId));
        }

        if (filters.Status.HasValue)
        {
            query = query.Where(i => i.Status == filters.Status.Value);
        }

        return query;
    }
}
