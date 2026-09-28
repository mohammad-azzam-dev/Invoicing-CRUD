using FluentValidation;
using InvoiceApp.Features.Account.Dtos;

namespace InvoiceApp.Features.Account.Validators;

public sealed class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword).Cascade(CascadeMode.Stop).ApplyPasswordRules();

        RuleFor(x => x.ConfirmPassword).ApplyConfirmPasswordRules(x => x.NewPassword);
    }
}
