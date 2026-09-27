namespace InvoiceApp.Domain;

public sealed class LineItem
{
    public int Id { get; private set; }
    public int InvoiceId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountPercent { get; private set; }

    // Navigation property (not stored)
    public Invoice Invoice { get; private set; } = null!;

    private LineItem() { }

    public static LineItem Create(
        int invoiceId,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal discountPercent
    )
    {
        return new LineItem
        {
            InvoiceId = invoiceId,
            Description = description.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountPercent = discountPercent,
        };
    }

    public void Update(
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal discountPercent
    )
    {
        Description = description.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountPercent = discountPercent;
    }
}
