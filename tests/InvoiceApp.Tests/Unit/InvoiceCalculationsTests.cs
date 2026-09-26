using InvoiceApp.Domain;
using Shouldly;

namespace InvoiceApp.Tests.Unit;

public sealed class InvoiceCalculationsTests
{
    private static readonly DateOnly Today = new(2024, 1, 15);

    #region LineItem Calculations

    [Fact]
    public void GrossAmount_ReturnsQuantityTimesUnitPrice()
    {
        var item = LineItem.Create(1, "Test", quantity: 5m, unitPrice: 100m, discountPercent: 0m);

        item.GrossAmount().ShouldBe(500m);
    }

    [Fact]
    public void GrossAmount_WithFractionalQuantity_ReturnsCorrectAmount()
    {
        var item = LineItem.Create(1, "Test", quantity: 2.5m, unitPrice: 40m, discountPercent: 0m);

        item.GrossAmount().ShouldBe(100m);
    }

    [Fact]
    public void DiscountAmount_WithZeroDiscount_ReturnsZero()
    {
        var item = LineItem.Create(1, "Test", quantity: 10m, unitPrice: 50m, discountPercent: 0m);

        item.DiscountAmount().ShouldBe(0m);
    }

    [Fact]
    public void DiscountAmount_With10Percent_ReturnsCorrectAmount()
    {
        var item = LineItem.Create(1, "Test", quantity: 10m, unitPrice: 50m, discountPercent: 10m);

        // GrossAmount = 500, DiscountAmount = 500 * 10 / 100 = 50
        item.DiscountAmount().ShouldBe(50m);
    }

    [Fact]
    public void DiscountAmount_With100Percent_ReturnsFullGrossAmount()
    {
        var item = LineItem.Create(1, "Test", quantity: 5m, unitPrice: 100m, discountPercent: 100m);

        // GrossAmount = 500, DiscountAmount = 500 * 100 / 100 = 500
        item.DiscountAmount().ShouldBe(500m);
    }

    [Fact]
    public void DiscountAmount_RoundsToTwoDecimalPlaces()
    {
        // GrossAmount = 3 * 33.33 = 99.99
        // DiscountAmount = 99.99 * 7 / 100 = 6.9993 -> rounds to 7.00
        var item = LineItem.Create(1, "Test", quantity: 3m, unitPrice: 33.33m, discountPercent: 7m);

        item.DiscountAmount().ShouldBe(7.00m);
    }

    [Fact]
    public void LineTotal_WithNoDiscount_ReturnsRoundedGrossAmount()
    {
        var item = LineItem.Create(1, "Test", quantity: 3m, unitPrice: 33.333m, discountPercent: 0m);

        // GrossAmount = 99.999, rounded = 100.00
        item.LineTotal().ShouldBe(100.00m);
    }

    [Fact]
    public void LineTotal_WithDiscount_ReturnsCorrectAmount()
    {
        var item = LineItem.Create(1, "Test", quantity: 10m, unitPrice: 50m, discountPercent: 10m);

        // GrossAmount = 500, rounded = 500
        // DiscountAmount = 50
        // LineTotal = 500 - 50 = 450
        item.LineTotal().ShouldBe(450m);
    }

    [Fact]
    public void LineTotal_With100PercentDiscount_ReturnsZero()
    {
        var item = LineItem.Create(1, "Test", quantity: 5m, unitPrice: 100m, discountPercent: 100m);

        item.LineTotal().ShouldBe(0m);
    }

    #endregion

    #region Invoice-level Calculations

    [Fact]
    public void Subtotal_WithMultipleItems_ReturnsSumOfLineTotals()
    {
        var items = new[]
        {
            LineItem.Create(1, "Item 1", quantity: 2m, unitPrice: 100m, discountPercent: 0m), // LineTotal = 200
            LineItem.Create(1, "Item 2", quantity: 3m, unitPrice: 50m, discountPercent: 10m), // LineTotal = 150 - 15 = 135
            LineItem.Create(1, "Item 3", quantity: 1m, unitPrice: 75m, discountPercent: 0m)   // LineTotal = 75
        };

        InvoiceCalculations.Subtotal(items).ShouldBe(410m);
    }

    [Fact]
    public void Subtotal_WithNoItems_ReturnsZero()
    {
        var items = Array.Empty<LineItem>();

        InvoiceCalculations.Subtotal(items).ShouldBe(0m);
    }

    [Fact]
    public void DiscountTotal_WithMultipleItems_ReturnsSumOfDiscountAmounts()
    {
        var items = new[]
        {
            LineItem.Create(1, "Item 1", quantity: 2m, unitPrice: 100m, discountPercent: 10m), // DiscountAmount = 20
            LineItem.Create(1, "Item 2", quantity: 3m, unitPrice: 50m, discountPercent: 20m), // DiscountAmount = 30
            LineItem.Create(1, "Item 3", quantity: 1m, unitPrice: 75m, discountPercent: 0m)   // DiscountAmount = 0
        };

        InvoiceCalculations.DiscountTotal(items).ShouldBe(50m);
    }

    [Fact]
    public void TaxAmount_With21PercentRate_ReturnsCorrectAmount()
    {
        var invoice = Invoice.Create(1, Today, Today.AddDays(30), taxRate: 21m).Value!;
        var items = new[]
        {
            LineItem.Create(1, "Item 1", quantity: 1m, unitPrice: 100m, discountPercent: 0m)
        };

        // Subtotal = 100, TaxAmount = 100 * 21 / 100 = 21
        InvoiceCalculations.TaxAmount(invoice, items).ShouldBe(21m);
    }

    [Fact]
    public void TaxAmount_WithZeroRate_ReturnsZero()
    {
        var invoice = Invoice.Create(1, Today, Today.AddDays(30), taxRate: 0m).Value!;
        var items = new[]
        {
            LineItem.Create(1, "Item 1", quantity: 1m, unitPrice: 100m, discountPercent: 0m)
        };

        InvoiceCalculations.TaxAmount(invoice, items).ShouldBe(0m);
    }

    [Fact]
    public void TaxAmount_RoundsToTwoDecimalPlaces()
    {
        var invoice = Invoice.Create(1, Today, Today.AddDays(30), taxRate: 7m).Value!;
        var items = new[]
        {
            LineItem.Create(1, "Item 1", quantity: 1m, unitPrice: 33.33m, discountPercent: 0m)
        };

        // Subtotal = 33.33, TaxAmount = 33.33 * 7 / 100 = 2.3331 -> 2.33
        InvoiceCalculations.TaxAmount(invoice, items).ShouldBe(2.33m);
    }

    [Fact]
    public void Total_ReturnsSubtotalPlusTaxAmount()
    {
        var invoice = Invoice.Create(1, Today, Today.AddDays(30), taxRate: 21m).Value!;
        var items = new[]
        {
            LineItem.Create(1, "Item 1", quantity: 1m, unitPrice: 100m, discountPercent: 0m)
        };

        // Subtotal = 100, TaxAmount = 21, Total = 121
        InvoiceCalculations.Total(invoice, items).ShouldBe(121m);
    }

    [Fact]
    public void Total_WithEmptyItems_ReturnsZero()
    {
        var invoice = Invoice.Create(1, Today, Today.AddDays(30), taxRate: 21m).Value!;
        var items = Array.Empty<LineItem>();

        InvoiceCalculations.Total(invoice, items).ShouldBe(0m);
    }

    [Fact]
    public void ItemCount_ReturnsNumberOfItems()
    {
        var items = new[]
        {
            LineItem.Create(1, "Item 1", quantity: 1m, unitPrice: 100m, discountPercent: 0m),
            LineItem.Create(1, "Item 2", quantity: 1m, unitPrice: 50m, discountPercent: 0m),
            LineItem.Create(1, "Item 3", quantity: 1m, unitPrice: 25m, discountPercent: 0m)
        };

        InvoiceCalculations.ItemCount(items).ShouldBe(3);
    }

    [Fact]
    public void ItemCount_WithNoItems_ReturnsZero()
    {
        var items = Array.Empty<LineItem>();

        InvoiceCalculations.ItemCount(items).ShouldBe(0);
    }

    #endregion

    #region IsOverdue

    [Fact]
    public void IsOverdue_SentInvoiceWithPastDueDate_ReturnsTrue()
    {
        var invoice = CreateSentInvoice(dueDate: Today.AddDays(-5));

        InvoiceCalculations.IsOverdue(invoice, Today).ShouldBeTrue();
    }

    [Fact]
    public void IsOverdue_SentInvoiceWithFutureDueDate_ReturnsFalse()
    {
        var invoice = CreateSentInvoice(dueDate: Today.AddDays(5));

        InvoiceCalculations.IsOverdue(invoice, Today).ShouldBeFalse();
    }

    [Fact]
    public void IsOverdue_SentInvoiceWithDueDateToday_ReturnsFalse()
    {
        var invoice = CreateSentInvoice(dueDate: Today);

        InvoiceCalculations.IsOverdue(invoice, Today).ShouldBeFalse();
    }

    [Fact]
    public void IsOverdue_DraftInvoiceWithPastDueDate_ReturnsFalse()
    {
        var invoice = Invoice.Create(1, Today.AddDays(-30), Today.AddDays(-5), 21m).Value!;

        InvoiceCalculations.IsOverdue(invoice, Today).ShouldBeFalse();
    }

    [Fact]
    public void IsOverdue_PaidInvoice_ReturnsFalse()
    {
        var invoice = CreateSentInvoice(dueDate: Today.AddDays(-5));
        invoice.MarkAsPaid();

        InvoiceCalculations.IsOverdue(invoice, Today).ShouldBeFalse();
    }

    [Fact]
    public void IsOverdue_CancelledInvoice_ReturnsFalse()
    {
        var invoice = CreateSentInvoice(dueDate: Today.AddDays(-5));
        invoice.Cancel();

        InvoiceCalculations.IsOverdue(invoice, Today).ShouldBeFalse();
    }

    #endregion

    private static Invoice CreateSentInvoice(DateOnly dueDate)
    {
        var invoice = Invoice.Create(1, Today.AddDays(-30), dueDate, 21m).Value!;
        invoice.MarkAsSent(1);
        return invoice;
    }
}
