using InvoiceApp.Domain;
using Shouldly;

namespace InvoiceApp.Tests.Unit;

public sealed class InvoiceCalculationsTests
{
    private static readonly DateOnly Today = new(2024, 1, 15);

    [Fact]
    public void GrossAmount_ReturnsQuantityTimesUnitPrice()
    {
        LineItem item = LineItem.Create(
            1,
            "Test",
            quantity: 5m,
            unitPrice: 100m,
            discountPercent: 0m
        );

        item.GrossAmount().ShouldBe(500m);
    }

    [Fact]
    public void GrossAmount_WithFractionalQuantity_ReturnsCorrectAmount()
    {
        LineItem item = LineItem.Create(
            1,
            "Test",
            quantity: 2.5m,
            unitPrice: 40m,
            discountPercent: 0m
        );

        item.GrossAmount().ShouldBe(100m);
    }

    [Fact]
    public void DiscountAmount_WithZeroDiscount_ReturnsZero()
    {
        LineItem item = LineItem.Create(
            1,
            "Test",
            quantity: 10m,
            unitPrice: 50m,
            discountPercent: 0m
        );

        item.DiscountAmount().ShouldBe(0m);
    }

    [Fact]
    public void DiscountAmount_With10Percent_ReturnsCorrectAmount()
    {
        LineItem item = LineItem.Create(
            1,
            "Test",
            quantity: 10m,
            unitPrice: 50m,
            discountPercent: 10m
        );

        item.DiscountAmount().ShouldBe(50m);
    }

    [Fact]
    public void DiscountAmount_With100Percent_ReturnsFullGrossAmount()
    {
        LineItem item = LineItem.Create(
            1,
            "Test",
            quantity: 5m,
            unitPrice: 100m,
            discountPercent: 100m
        );

        item.DiscountAmount().ShouldBe(500m);
    }

    [Fact]
    public void DiscountAmount_RoundsToTwoDecimalPlaces()
    {
        LineItem item = LineItem.Create(
            1,
            "Test",
            quantity: 3m,
            unitPrice: 33.33m,
            discountPercent: 7m
        );

        item.DiscountAmount().ShouldBe(7.00m);
    }

    [Fact]
    public void LineTotal_WithNoDiscount_ReturnsRoundedGrossAmount()
    {
        LineItem item = LineItem.Create(
            1,
            "Test",
            quantity: 3m,
            unitPrice: 33.333m,
            discountPercent: 0m
        );

        item.LineTotal().ShouldBe(100.00m);
    }

    [Fact]
    public void LineTotal_WithDiscount_ReturnsCorrectAmount()
    {
        LineItem item = LineItem.Create(
            1,
            "Test",
            quantity: 10m,
            unitPrice: 50m,
            discountPercent: 10m
        );

        item.LineTotal().ShouldBe(450m);
    }

    [Fact]
    public void LineTotal_With100PercentDiscount_ReturnsZero()
    {
        LineItem item = LineItem.Create(
            1,
            "Test",
            quantity: 5m,
            unitPrice: 100m,
            discountPercent: 100m
        );

        item.LineTotal().ShouldBe(0m);
    }

    [Fact]
    public void Subtotal_WithMultipleItems_ReturnsSumOfLineTotals()
    {
        LineItem[] items =
        [
            LineItem.Create(1, "Item 1", quantity: 2m, unitPrice: 100m, discountPercent: 0m),
            LineItem.Create(1, "Item 2", quantity: 3m, unitPrice: 50m, discountPercent: 10m),
            LineItem.Create(1, "Item 3", quantity: 1m, unitPrice: 75m, discountPercent: 0m),
        ];

        InvoiceCalculations.Subtotal(items).ShouldBe(410m);
    }

    [Fact]
    public void Subtotal_WithNoItems_ReturnsZero()
    {
        LineItem[] items = [];

        InvoiceCalculations.Subtotal(items).ShouldBe(0m);
    }

    [Fact]
    public void DiscountTotal_WithMultipleItems_ReturnsSumOfDiscountAmounts()
    {
        LineItem[] items =
        [
            LineItem.Create(1, "Item 1", quantity: 2m, unitPrice: 100m, discountPercent: 10m),
            LineItem.Create(1, "Item 2", quantity: 3m, unitPrice: 50m, discountPercent: 20m),
            LineItem.Create(1, "Item 3", quantity: 1m, unitPrice: 75m, discountPercent: 0m),
        ];

        InvoiceCalculations.DiscountTotal(items).ShouldBe(50m);
    }

    [Fact]
    public void TaxAmount_With21PercentRate_ReturnsCorrectAmount()
    {
        Invoice invoice = Invoice.Create(1, Today, Today.AddDays(30), taxRate: 21m).Value!;
        LineItem[] items =
        [
            LineItem.Create(1, "Item 1", quantity: 1m, unitPrice: 100m, discountPercent: 0m),
        ];

        InvoiceCalculations.TaxAmount(invoice, items).ShouldBe(21m);
    }

    [Fact]
    public void TaxAmount_WithZeroRate_ReturnsZero()
    {
        Invoice invoice = Invoice.Create(1, Today, Today.AddDays(30), taxRate: 0m).Value!;
        LineItem[] items =
        [
            LineItem.Create(1, "Item 1", quantity: 1m, unitPrice: 100m, discountPercent: 0m),
        ];

        InvoiceCalculations.TaxAmount(invoice, items).ShouldBe(0m);
    }

    [Fact]
    public void TaxAmount_RoundsToTwoDecimalPlaces()
    {
        Invoice invoice = Invoice.Create(1, Today, Today.AddDays(30), taxRate: 7m).Value!;
        LineItem[] items =
        [
            LineItem.Create(1, "Item 1", quantity: 1m, unitPrice: 33.33m, discountPercent: 0m),
        ];

        InvoiceCalculations.TaxAmount(invoice, items).ShouldBe(2.33m);
    }

    [Fact]
    public void Total_ReturnsSubtotalPlusTaxAmount()
    {
        Invoice invoice = Invoice.Create(1, Today, Today.AddDays(30), taxRate: 21m).Value!;
        LineItem[] items =
        [
            LineItem.Create(1, "Item 1", quantity: 1m, unitPrice: 100m, discountPercent: 0m),
        ];

        InvoiceCalculations.Total(invoice, items).ShouldBe(121m);
    }

    [Fact]
    public void Total_WithEmptyItems_ReturnsZero()
    {
        Invoice invoice = Invoice.Create(1, Today, Today.AddDays(30), taxRate: 21m).Value!;
        LineItem[] items = [];

        InvoiceCalculations.Total(invoice, items).ShouldBe(0m);
    }

    [Fact]
    public void ItemCount_ReturnsNumberOfItems()
    {
        LineItem[] items =
        [
            LineItem.Create(1, "Item 1", quantity: 1m, unitPrice: 100m, discountPercent: 0m),
            LineItem.Create(1, "Item 2", quantity: 1m, unitPrice: 50m, discountPercent: 0m),
            LineItem.Create(1, "Item 3", quantity: 1m, unitPrice: 25m, discountPercent: 0m),
        ];

        InvoiceCalculations.ItemCount(items).ShouldBe(3);
    }

    [Fact]
    public void ItemCount_WithNoItems_ReturnsZero()
    {
        LineItem[] items = [];

        InvoiceCalculations.ItemCount(items).ShouldBe(0);
    }

    [Fact]
    public void IsOverdue_SentInvoiceWithPastDueDate_ReturnsTrue()
    {
        Invoice invoice = CreateSentInvoice(dueDate: Today.AddDays(-5));

        InvoiceCalculations.IsOverdue(invoice, Today).ShouldBeTrue();
    }

    [Fact]
    public void IsOverdue_SentInvoiceWithFutureDueDate_ReturnsFalse()
    {
        Invoice invoice = CreateSentInvoice(dueDate: Today.AddDays(5));

        InvoiceCalculations.IsOverdue(invoice, Today).ShouldBeFalse();
    }

    [Fact]
    public void IsOverdue_SentInvoiceWithDueDateToday_ReturnsFalse()
    {
        Invoice invoice = CreateSentInvoice(dueDate: Today);

        InvoiceCalculations.IsOverdue(invoice, Today).ShouldBeFalse();
    }

    [Fact]
    public void IsOverdue_DraftInvoiceWithPastDueDate_ReturnsFalse()
    {
        Invoice invoice = Invoice.Create(1, Today.AddDays(-30), Today.AddDays(-5), 21m).Value!;

        InvoiceCalculations.IsOverdue(invoice, Today).ShouldBeFalse();
    }

    [Fact]
    public void IsOverdue_PaidInvoice_ReturnsFalse()
    {
        Invoice invoice = CreateSentInvoice(dueDate: Today.AddDays(-5));
        invoice.MarkAsPaid();

        InvoiceCalculations.IsOverdue(invoice, Today).ShouldBeFalse();
    }

    [Fact]
    public void IsOverdue_CancelledInvoice_ReturnsFalse()
    {
        Invoice invoice = CreateSentInvoice(dueDate: Today.AddDays(-5));
        invoice.Cancel();

        InvoiceCalculations.IsOverdue(invoice, Today).ShouldBeFalse();
    }

    private static Invoice CreateSentInvoice(DateOnly dueDate)
    {
        Invoice invoice = Invoice.Create(1, Today.AddDays(-30), dueDate, 21m).Value!;
        invoice.MarkAsSent(1);
        return invoice;
    }
}
