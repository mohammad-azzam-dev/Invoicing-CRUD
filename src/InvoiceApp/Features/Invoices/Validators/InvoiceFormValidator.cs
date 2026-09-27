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

        return Result.Success();
    }
}
