using FluentValidation;
using FluentValidation.Results;
using InvoiceApp.Domain;
using InvoiceApp.Features.Customers.Dtos;
using InvoiceApp.Features.Customers.Interfaces;
using InvoiceApp.Features.Invoices;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Features.Customers.Services;

public sealed class CustomerService(
    ICustomerRepository repository,
    IValidator<CustomerFormDto> formValidator,
    IValidator<CustomerDeleteDto> deleteValidator,
    ILogger<CustomerService> logger
) : ICustomerService
{
    public async Task<PagedResult<CustomerListItemDto>> GetPagedAsync(
        CustomerQuery query,
        CancellationToken ct = default
    )
    {
        logger.LogDebug(
            "Getting paged customers: Page={Page}, PageSize={PageSize}, Search={Search}",
            query.Page,
            query.PageSize,
            query.Search
        );
        return await repository.GetPagedAsync(query, ct);
    }

    public async Task<CustomerDetailsDto?> GetDetailsAsync(int id, CancellationToken ct = default)
    {
        CustomerWithInvoiceCount? result = await repository.GetByIdWithInvoiceCountAsync(id, ct);

        if (result is null)
        {
            logger.LogDebug("Customer not found: {CustomerId}", id);
            return null;
        }

        return CustomerMappings.ToDetailsDto(result.Customer, result.InvoiceCount);
    }

    public async Task<Result<int>> CreateAsync(CustomerFormDto form, CancellationToken ct = default)
    {
        ValidationResult validationResult = await formValidator.ValidateAsync(form, ct);
        if (!validationResult.IsValid)
        {
            string error = validationResult.Errors.First().ErrorMessage;
            logger.LogWarning("Validation failed for new customer: {Error}", error);
            return Result<int>.Failure(error);
        }

        try
        {
            Customer customer = Customer.Create(
                form.Name,
                form.Phone,
                form.Email,
                form.CompanyName,
                form.Address
            );

            int customerId = await repository.AddAsync(customer, ct);

            logger.LogInformation("Created customer {CustomerId}: {Name}", customerId, form.Name);

            return Result<int>.Success(customerId);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error while creating customer");
            return Result<int>.Failure("Failed to create customer. Please try again.");
        }
    }

    public async Task<Result> UpdateAsync(int id, CustomerFormDto form, CancellationToken ct = default)
    {
        ValidationResult validationResult = await formValidator.ValidateAsync(form, ct);
        if (!validationResult.IsValid)
        {
            string error = validationResult.Errors.First().ErrorMessage;
            logger.LogWarning("Validation failed for customer {CustomerId}: {Error}", id, error);
            return Result.Failure(error);
        }

        Customer? customer = await repository.GetByIdAsync(id, ct);
        if (customer is null)
        {
            logger.LogWarning("Attempted to update non-existent customer: {CustomerId}", id);
            return Result.Failure("Customer not found.");
        }

        try
        {
            customer.Update(form.Name, form.Phone, form.Email, form.CompanyName, form.Address);
            await repository.UpdateAsync(customer, ct);

            logger.LogInformation("Updated customer {CustomerId}", id);

            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error while updating customer {CustomerId}", id);
            return Result.Failure("Failed to update customer. Please try again.");
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        CustomerDeleteDto deleteDto = new(id);
        ValidationResult validationResult = await deleteValidator.ValidateAsync(deleteDto, ct);

        if (!validationResult.IsValid)
        {
            string error = validationResult.Errors.First().ErrorMessage;
            logger.LogWarning("Delete validation failed for customer {CustomerId}: {Error}", id, error);
            return Result.Failure(error);
        }

        Customer? customer = await repository.GetByIdAsync(id, ct);
        if (customer is null)
        {
            // This shouldn't happen since validator already checked, but handle gracefully
            return Result.Failure("Customer not found.");
        }

        try
        {
            await repository.DeleteAsync(customer, ct);
            logger.LogInformation("Deleted customer {CustomerId}", id);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error while deleting customer {CustomerId}", id);
            return Result.Failure("Failed to delete customer. Please try again.");
        }
    }
}
