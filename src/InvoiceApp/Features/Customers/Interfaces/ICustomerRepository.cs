using InvoiceApp.Domain;
using InvoiceApp.Features.Customers.Dtos;
using InvoiceApp.Features.Invoices;

namespace InvoiceApp.Features.Customers.Interfaces;

public interface ICustomerRepository
{
    Task<PagedResult<CustomerListItemDto>> GetPagedAsync(
        CustomerQuery query,
        CancellationToken ct = default
    );

    Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<CustomerWithInvoiceCount?> GetByIdWithInvoiceCountAsync(
        int id,
        CancellationToken ct = default
    );

    Task<bool> EmailExistsAsync(string email, int? excludeId = null, CancellationToken ct = default);

    Task<int> AddAsync(Customer customer, CancellationToken ct = default);

    Task UpdateAsync(Customer customer, CancellationToken ct = default);

    Task DeleteAsync(Customer customer, CancellationToken ct = default);
}
