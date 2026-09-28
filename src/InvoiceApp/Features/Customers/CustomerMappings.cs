using InvoiceApp.Domain;
using InvoiceApp.Features.Customers.Dtos;

namespace InvoiceApp.Features.Customers;

public static class CustomerMappings
{
    public static string GetDisplayName(Customer customer) =>
        string.IsNullOrWhiteSpace(customer.CompanyName) ? customer.Name : customer.CompanyName;

    public static string GetDisplayName(string name, string? companyName) =>
        string.IsNullOrWhiteSpace(companyName) ? name : companyName;

    public static CustomerDetailsDto ToDetailsDto(Customer customer, int invoiceCount) =>
        new(
            Id: customer.Id,
            Name: customer.Name,
            CompanyName: customer.CompanyName,
            Address: customer.Address,
            Phone: customer.Phone,
            Email: customer.Email,
            InvoiceCount: invoiceCount
        );
}
