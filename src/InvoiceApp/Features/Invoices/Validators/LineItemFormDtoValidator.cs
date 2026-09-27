using FluentValidation;
using InvoiceApp.Features.Invoices.Dtos;

namespace InvoiceApp.Features.Invoices.Validators;

public class LineItemFormDtoValidator : AbstractValidator<LineItemFormDto>
{
    public LineItemFormDtoValidator()
    {
        RuleFor(inputs => inputs.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(200)
            .WithMessage("Description cannot exceed 200 characters.");

        RuleFor(inputs => inputs.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0.")
            .PrecisionScale(18, 2, true)
            .WithMessage("Quantity cannot have more than 2 decimal places.");

        RuleFor(inputs => inputs.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Unit price cannot be negative.")
            .PrecisionScale(18, 2, true)
            .WithMessage("Unit price cannot have more than 2 decimal places.");

        RuleFor(inputs => inputs.DiscountPercent)
            .InclusiveBetween(0, 100)
            .WithMessage("Discount must be between 0 and 100.");
    }
}
