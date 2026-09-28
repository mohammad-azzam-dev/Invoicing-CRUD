using FluentValidation;
using InvoiceApp.Features.Customers.Dtos;
using InvoiceApp.Features.Customers.Interfaces;

namespace InvoiceApp.Features.Customers.Validators;

public sealed class CustomerFormDtoValidator : AbstractValidator<CustomerFormDto>
{
    public CustomerFormDtoValidator(ICustomerRepository repository)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(100)
            .WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email must be a valid email address.")
            .MaximumLength(150)
            .WithMessage("Email must not exceed 150 characters.")
            .MustAsync(
                async (dto, email, ct) => !await repository.EmailExistsAsync(email, dto.Id, ct)
            )
            .WithMessage("A customer with this email already exists.");

        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithMessage("Phone is required.")
            .MaximumLength(30)
            .WithMessage("Phone must not exceed 30 characters.");

        RuleFor(x => x.CompanyName)
            .MaximumLength(150)
            .WithMessage("Company name must not exceed 150 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(300)
            .WithMessage("Address must not exceed 300 characters.");
    }
}
