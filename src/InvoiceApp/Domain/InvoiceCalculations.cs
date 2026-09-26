namespace InvoiceApp.Domain;

public static class InvoiceCalculations
{
    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static decimal GrossAmount(this LineItem item) =>
        item.Quantity * item.UnitPrice;

    public static decimal DiscountAmount(this LineItem item) =>
        Round(item.GrossAmount() * item.DiscountPercent / 100);

    public static decimal LineTotal(this LineItem item) =>
        Round(item.GrossAmount()) - item.DiscountAmount();

    public static decimal Subtotal(IEnumerable<LineItem> items) =>
        items.Sum(item => item.LineTotal());

    public static decimal DiscountTotal(IEnumerable<LineItem> items) =>
        items.Sum(item => item.DiscountAmount());

    public static decimal TaxAmount(Invoice invoice, IEnumerable<LineItem> items) =>
        Round(Subtotal(items) * invoice.TaxRate / 100);

    public static decimal Total(Invoice invoice, IEnumerable<LineItem> items) =>
        Subtotal(items) + TaxAmount(invoice, items);

    public static int ItemCount(IEnumerable<LineItem> items) =>
        items.Count();

    public static bool IsOverdue(Invoice invoice, DateOnly today) =>
        invoice.Status == InvoiceStatus.Sent && invoice.DueDate < today;
}
