using FluentValidation;
using InvoiceApp.Features.Customers.Dtos;
using InvoiceApp.Features.Customers.Interfaces;

namespace InvoiceApp.Features.Customers.Validators;

public sealed class CustomerDeleteDtoValidator : AbstractValidator<CustomerDeleteDto>
{
    private readonly ICustomerRepository _repository;
    private CustomerWithInvoiceCount? _cachedResult;

    public CustomerDeleteDtoValidator(ICustomerRepository repository)
    {
        _repository = repository;

        RuleFor(x => x.Id)
            .MustAsync(CustomerExistsAsync)
            .WithMessage("Customer not found.")
            .MustAsync(CustomerHasNoInvoicesAsync)
            .WithMessage(GetInvoiceCountMessage);
    }

    private async Task<bool> CustomerExistsAsync(int id, CancellationToken ct)
    {
        _cachedResult = await _repository.GetByIdWithInvoiceCountAsync(id, ct);
        return _cachedResult is not null;
    }

    private Task<bool> CustomerHasNoInvoicesAsync(int id, CancellationToken ct)
    {
        // This is called after CustomerExistsAsync, so _cachedResult is populated
        // If customer doesn't exist, the first rule already failed
        if (_cachedResult is null)
        {
            return Task.FromResult(true); // Skip this check, first rule handles it
        }

        return Task.FromResult(_cachedResult.InvoiceCount == 0);
    }

    private string GetInvoiceCountMessage(CustomerDeleteDto dto)
    {
        int count = _cachedResult?.InvoiceCount ?? 0;
        return $"This customer has {count} invoice(s) and can't be deleted.";
    }
}
