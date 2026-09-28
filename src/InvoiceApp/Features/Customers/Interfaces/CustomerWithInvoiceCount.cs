using InvoiceApp.Domain;

namespace InvoiceApp.Features.Customers.Interfaces;

public sealed record CustomerWithInvoiceCount(Customer Customer, int InvoiceCount);
