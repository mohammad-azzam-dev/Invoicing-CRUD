using Microsoft.EntityFrameworkCore;
using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Interfaces;

namespace InvoiceApp.Features.Invoices.Services;

public sealed class InvoiceService(
    IInvoiceRepository repository,
    ILogger<InvoiceService> logger) : IInvoiceService
{
    public async Task<PagedResult<InvoiceListItemDto>> GetPagedAsync(InvoiceQuery query, CancellationToken ct = default)
    {
        logger.LogDebug("Getting paged invoices: Page={Page}, PageSize={PageSize}, Search={Search}, Status={Status}",
            query.Page, query.PageSize, query.Search, query.Status);
        return await repository.GetPagedAsync(query, ct);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        Invoice? invoice = await repository.GetByIdAsync(id, ct);

        if (invoice is null)
        {
            logger.LogWarning("Attempted to delete non-existent invoice: {InvoiceId}", id);
            return Result.Failure("Invoice not found.");
        }

        if (!invoice.CanDelete())
        {
            logger.LogWarning("Attempted to delete non-draft invoice: {InvoiceId}, Status={Status}", id, invoice.Status);
            return Result.Failure("Only draft invoices can be deleted.");
        }

        try
        {
            await repository.DeleteAsync(invoice, ct);
            logger.LogInformation("Deleted invoice: {InvoiceId}", id);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error while deleting invoice: {InvoiceId}", id);
            return Result.Failure("Failed to delete invoice. Please try again.");
        }
    }
}
