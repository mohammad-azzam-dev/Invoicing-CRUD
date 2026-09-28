using FluentValidation;
using FluentValidation.Results;
using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;

namespace InvoiceApp.Features.Invoices.Validators;

public sealed class InvoiceFormValidator(
    IValidator<InvoiceFormDto> invoiceValidator,
    IValidator<LineItemFormDto> lineItemValidator,
    ILogger<InvoiceFormValidator> logger
)
{
    public async Task<Result> ValidateAsync(
        InvoiceFormDto form,
        IReadOnlyList<LineItemFormDto> lineItems,
        CancellationToken ct = default
    )
    {
        if (lineItems.Count == 0)
        {
            logger.LogWarning(
                "Invoice validation failed: Invoice must have at least one line item"
            );
            return Result.Failure("Invoice must have at least one line item.");
        }

        ValidationResult invoiceValidation = await invoiceValidator.ValidateAsync(form, ct);
        if (!invoiceValidation.IsValid)
        {
            string errors = string.Join("; ", invoiceValidation.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning("Invoice validation failed: {Errors}", errors);
            return Result.Failure(errors);
        }

        foreach (LineItemFormDto lineItem in lineItems)
        {
            ValidationResult lineValidation = await lineItemValidator.ValidateAsync(lineItem, ct);
            if (!lineValidation.IsValid)
            {
                string errors = string.Join(
                    "; ",
                    lineValidation.Errors.Select(e => e.ErrorMessage)
                );
                logger.LogWarning("Line item validation failed: {Errors}", errors);
                return Result.Failure(errors);
            }
        }

        IEnumerable<decimal> lineTotals = lineItems.Select(item =>
            InvoiceCalculations.CalculateLineTotal(
                item.Quantity,
                item.UnitPrice,
                item.DiscountPercent
            )
        );
        decimal subtotal = InvoiceCalculations.CalculateSubtotal(lineTotals);
        decimal total = InvoiceCalculations.CalculateTotal(subtotal, form.TaxRate);

        if (total <= 0)
        {
            logger.LogWarning("Invoice validation failed: Invoice total must be greater than 0");
            return Result.Failure("Invoice total must be greater than 0.");
        }

        return Result.Success();
    }
}
