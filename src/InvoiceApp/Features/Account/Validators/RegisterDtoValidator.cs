using FluentValidation;
using InvoiceApp.Features.Account.Dtos;

namespace InvoiceApp.Features.Account.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(fields => fields.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(fields => fields.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(fields => fields.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required.")
            .Equal(fields => fields.Password).WithMessage("Passwords do not match.");
    }
}
