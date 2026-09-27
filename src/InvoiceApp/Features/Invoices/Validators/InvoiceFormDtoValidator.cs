using FluentValidation;
using InvoiceApp.Features.Invoices.Dtos;

namespace InvoiceApp.Features.Invoices.Validators;

public class InvoiceFormDtoValidator : AbstractValidator<InvoiceFormDto>
{
    public InvoiceFormDtoValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Customer is required.");

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.IssueDate)
            .WithMessage("Due date must be on or after the issue date.");

        RuleFor(x => x.TaxRate)
            .InclusiveBetween(0, 100)
            .WithMessage("Tax rate must be between 0 and 100.");
    }
}
