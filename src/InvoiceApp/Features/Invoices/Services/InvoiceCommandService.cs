using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Interfaces;
using InvoiceApp.Features.Invoices.Validators;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Features.Invoices.Services;

public sealed class InvoiceCommandService(
    IInvoiceRepository repository,
    InvoiceFormValidator formValidator,
    ILogger<InvoiceCommandService> logger
) : IInvoiceCommandService
{
    public async Task<Result<int>> SaveAsync(
        int id,
        InvoiceFormDto form,
        IReadOnlyList<LineItemFormDto> lineItems,
        CancellationToken ct = default
    )
    {
        Result validationResult = await formValidator.ValidateAsync(form, lineItems, ct);
        if (!validationResult.IsSuccess)
        {
            return Result<int>.Failure(validationResult.Error!);
        }

        try
        {
            Result<(Invoice, ICollection<LineItem>)> prepareResult = await PrepareInvoiceDataAsync(
                id,
                form,
                ct
            );

            if (!prepareResult.IsSuccess)
            {
                return Result<int>.Failure(prepareResult.Error!);
            }

            (Invoice invoice, ICollection<LineItem> existingLineItems) = prepareResult.Value;

            int invoiceId = await repository.UpsertInvoiceAsync(
                invoice,
                existingLineItems,
                lineItems,
                ct
            );

            logger.LogInformation(
                "{Action} invoice {InvoiceId} with {LineItemCount} line items",
                id == 0 ? "Created" : "Updated",
                invoiceId,
                lineItems.Count
            );

            return Result<int>.Success(invoiceId);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency error while saving invoice: {InvoiceId}", id);
            return Result<int>.Failure(
                "The invoice was modified by another user. Please refresh and try again."
            );
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error while saving invoice: {InvoiceId}", id);
            return Result<int>.Failure("Failed to save invoice. Please try again.");
        }
    }

    public async Task<Result> ChangeStatusAsync(
        int id,
        InvoiceStatus newStatus,
        CancellationToken ct = default
    )
    {
        (Invoice? invoice, int itemCount) = await repository.GetWithLineItemCountAsync(id, ct);

        if (invoice is null)
        {
            logger.LogWarning(
                "Attempted to change status of non-existent invoice: {InvoiceId}",
                id
            );
            return Result.Failure("Invoice not found.");
        }

        Result transitionResult = newStatus switch
        {
            InvoiceStatus.Sent => invoice.MarkAsSent(itemCount),
            InvoiceStatus.Paid => invoice.MarkAsPaid(),
            InvoiceStatus.Cancelled => invoice.Cancel(),
            _ => Result.Failure($"Cannot transition to {newStatus}."),
        };

        if (!transitionResult.IsSuccess)
        {
            logger.LogWarning(
                "Status transition failed for invoice {InvoiceId}: {Error}",
                id,
                transitionResult.Error
            );
            return transitionResult;
        }

        try
        {
            await repository.UpdateAsync(invoice, ct);
            logger.LogInformation(
                "Changed status of invoice {InvoiceId} to {NewStatus}",
                id,
                newStatus
            );
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency error while updating invoice status: {InvoiceId}", id);
            return Result.Failure(
                "The invoice was modified by another user. Please refresh and try again."
            );
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error while updating invoice status: {InvoiceId}", id);
            return Result.Failure("Failed to update invoice status. Please try again.");
        }
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
            logger.LogWarning(
                "Attempted to delete non-draft invoice: {InvoiceId}, Status={Status}",
                id,
                invoice.Status
            );
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

    private async Task<Result<(Invoice, ICollection<LineItem>)>> PrepareInvoiceDataAsync(
        int id,
        InvoiceFormDto form,
        CancellationToken ct
    )
    {
        if (id == 0)
        {
            Result<Invoice> createResult = Invoice.Create(
                form.CustomerId,
                form.IssueDate,
                form.DueDate,
                form.TaxRate
            );

            if (!createResult.IsSuccess)
            {
                return Result<(Invoice, ICollection<LineItem>)>.Failure(createResult.Error!);
            }

            return Result<(Invoice, ICollection<LineItem>)>.Success((createResult.Value!, []));
        }

        Invoice? existing = await repository.GetForEditAsync(id, ct);

        if (existing is null)
        {
            logger.LogWarning("Attempted to update non-existent invoice: {InvoiceId}", id);
            return Result<(Invoice, ICollection<LineItem>)>.Failure("Invoice not found.");
        }

        if (!existing.CanEdit())
        {
            logger.LogWarning(
                "Attempted to edit non-draft invoice: {InvoiceId}, Status={Status}",
                id,
                existing.Status
            );
            return Result<(Invoice, ICollection<LineItem>)>.Failure(
                "Only draft invoices can be edited."
            );
        }

        Result updateResult = existing.Update(
            form.CustomerId,
            form.IssueDate,
            form.DueDate,
            form.TaxRate
        );

        if (!updateResult.IsSuccess)
        {
            return Result<(Invoice, ICollection<LineItem>)>.Failure(updateResult.Error!);
        }

        return Result<(Invoice, ICollection<LineItem>)>.Success((existing, existing.LineItems));
    }
}
