using FluentValidation;
using InvoiceApp.Features.Account.Dtos;

namespace InvoiceApp.Features.Account.Validators;

public sealed class ChangeEmailDtoValidator : AbstractValidator<ChangeEmailDto>
{
    public ChangeEmailDtoValidator()
    {
        RuleFor(x => x.NewEmail)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email must be a valid email address.")
            .MaximumLength(256)
            .WithMessage("Email must not exceed 256 characters.");
    }
}
