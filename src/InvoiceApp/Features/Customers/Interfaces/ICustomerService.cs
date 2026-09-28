using InvoiceApp.Domain;
using InvoiceApp.Features.Customers.Dtos;
using InvoiceApp.Features.Invoices;

namespace InvoiceApp.Features.Customers.Interfaces;

public interface ICustomerService
{
    Task<PagedResult<CustomerListItemDto>> GetPagedAsync(
        CustomerQuery query,
        CancellationToken ct = default
    );

    Task<CustomerDetailsDto?> GetDetailsAsync(int id, CancellationToken ct = default);

    Task<Result<int>> CreateAsync(CustomerFormDto form, CancellationToken ct = default);

    Task<Result> UpdateAsync(int id, CustomerFormDto form, CancellationToken ct = default);

    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}
