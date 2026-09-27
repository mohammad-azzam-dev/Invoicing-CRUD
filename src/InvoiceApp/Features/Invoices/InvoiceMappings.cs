using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;

namespace InvoiceApp.Features.Invoices;

public static class InvoiceMappings
{
    public static LineItemDto ToDto(this LineItem item) =>
        new(
            Id: item.Id,
            Description: item.Description,
            Quantity: item.Quantity,
            UnitPrice: item.UnitPrice,
            DiscountPercent: item.DiscountPercent,
            LineTotal: item.LineTotal()
        );

    public static InvoiceDetailsDto ToDetailsDto(
        this Invoice invoice,
        IEnumerable<LineItem> lineItems
    )
    {
        List<LineItem> items = lineItems.ToList();
        IReadOnlyList<LineItemDto> lineItemDtos = items.Select(i => i.ToDto()).ToList();

        return new InvoiceDetailsDto(
            Id: invoice.Id,
            Number: InvoiceNumber.Format(invoice.Id),
            CustomerId: invoice.CustomerId,
            CustomerName: GetCustomerDisplayName(invoice.Customer),
            IssueDate: invoice.IssueDate,
            DueDate: invoice.DueDate,
            Status: invoice.Status,
            TaxRate: invoice.TaxRate,
            CanEdit: invoice.CanEdit(),
            LineItems: lineItemDtos,
            Subtotal: InvoiceCalculations.Subtotal(items),
            DiscountTotal: InvoiceCalculations.DiscountTotal(items),
            TaxAmount: InvoiceCalculations.TaxAmount(invoice, items),
            Total: InvoiceCalculations.Total(invoice, items)
        );
    }

    public static CustomerLookupDto ToLookupDto(this Customer customer) =>
        new(customer.Id, GetCustomerDisplayName(customer));

    public static string GetCustomerDisplayName(Customer customer) =>
        string.IsNullOrWhiteSpace(customer.CompanyName) ? customer.Name : customer.CompanyName;
}
