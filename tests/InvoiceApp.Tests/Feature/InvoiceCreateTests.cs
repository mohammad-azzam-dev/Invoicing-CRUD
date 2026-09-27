using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Tests.TestSupport;
using Shouldly;

namespace InvoiceApp.Tests.Feature;

public sealed class InvoiceCreateTests
{
    private static readonly DateOnly FixedToday = InvoiceTestHelper.FixedToday;

    [Fact]
    public async Task Create_ValidInvoiceWithThreeLineItems_SavesAllFieldsCorrectly()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync("John Doe", "Acme Corp");

        InvoiceFormDto form = new(customerId, FixedToday, FixedToday.AddDays(30), 15.5m);
        List<LineItemFormDto> lineItems =
        [
            new(0, "Widget A", 2.5m, 100.00m, 10m),
            new(0, "Widget B", 1m, 250.50m, 0m),
            new(0, "Widget C", 3m, 75.25m, 5.5m),
        ];

        // Act
        Result<int> result = await helper.CommandService.SaveAsync(0, form, lineItems);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        int invoiceId = result.Value;

        Invoice? reloaded = await helper.ReloadInvoiceAsync(invoiceId);
        reloaded.ShouldNotBeNull();
        reloaded.Status.ShouldBe(InvoiceStatus.Draft);
        reloaded.CustomerId.ShouldBe(customerId);
        reloaded.IssueDate.ShouldBe(FixedToday);
        reloaded.DueDate.ShouldBe(FixedToday.AddDays(30));
        reloaded.TaxRate.ShouldBe(15.5m);

        reloaded.LineItems.Count.ShouldBe(3);

        LineItem widgetA = reloaded.LineItems.Single(l => l.Description == "Widget A");
        widgetA.Quantity.ShouldBe(2.5m);
        widgetA.UnitPrice.ShouldBe(100.00m);
        widgetA.DiscountPercent.ShouldBe(10m);

        LineItem widgetB = reloaded.LineItems.Single(l => l.Description == "Widget B");
        widgetB.Quantity.ShouldBe(1m);
        widgetB.UnitPrice.ShouldBe(250.50m);
        widgetB.DiscountPercent.ShouldBe(0m);

        LineItem widgetC = reloaded.LineItems.Single(l => l.Description == "Widget C");
        widgetC.Quantity.ShouldBe(3m);
        widgetC.UnitPrice.ShouldBe(75.25m);
        widgetC.DiscountPercent.ShouldBe(5.5m);
    }

    [Fact]
    public async Task Create_ReturnedIdLoadsInvoice_WithCorrectNumberFormat()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        InvoiceFormDto form = InvoiceTestHelper.CreateValidForm(customerId);
        List<LineItemFormDto> lineItems = [InvoiceTestHelper.CreateValidLineItem()];

        // Act
        Result<int> result = await helper.CommandService.SaveAsync(0, form, lineItems);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        int invoiceId = result.Value;

        InvoiceDetailsDto? details = await helper.QueryService.GetDetailsAsync(invoiceId);
        details.ShouldNotBeNull();
        details.Number.ShouldBe($"INV-{invoiceId:D5}");
    }

    [Fact]
    public async Task Create_TotalsAfterReload_MatchInvoiceCalculations()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        decimal taxRate = 20m;
        InvoiceFormDto form = new(customerId, FixedToday, FixedToday.AddDays(30), taxRate);
        List<LineItemFormDto> lineItems =
        [
            new(0, "Item 1", 2m, 100m, 10m),  // Gross=200, Discount=20, LineTotal=180
            new(0, "Item 2", 1m, 50m, 0m),    // Gross=50, Discount=0, LineTotal=50
            new(0, "Item 3", 3m, 33.33m, 5m), // Gross=99.99, Discount=5.00, LineTotal=94.99
        ];

        // Act
        Result<int> result = await helper.CommandService.SaveAsync(0, form, lineItems);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Invoice? reloaded = await helper.ReloadInvoiceAsync(result.Value);
        reloaded.ShouldNotBeNull();

        // Calculate expected totals using Domain calculations
        decimal expectedSubtotal = InvoiceCalculations.Subtotal(reloaded.LineItems);
        decimal expectedTaxAmount = InvoiceCalculations.TaxAmount(reloaded, reloaded.LineItems);
        decimal expectedTotal = InvoiceCalculations.Total(reloaded, reloaded.LineItems);

        // Verify via details DTO
        InvoiceDetailsDto? details = await helper.QueryService.GetDetailsAsync(result.Value);
        details.ShouldNotBeNull();
        details.Subtotal.ShouldBe(expectedSubtotal);
        details.TaxAmount.ShouldBe(expectedTaxAmount);
        details.Total.ShouldBe(expectedTotal);
    }

    [Fact]
    public async Task Create_WithNoLineItems_AllowedAsDraft()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        InvoiceFormDto form = InvoiceTestHelper.CreateValidForm(customerId);
        List<LineItemFormDto> lineItems = [];

        // Act
        Result<int> result = await helper.CommandService.SaveAsync(0, form, lineItems);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Invoice? reloaded = await helper.ReloadInvoiceAsync(result.Value);
        reloaded.ShouldNotBeNull();
        reloaded.Status.ShouldBe(InvoiceStatus.Draft);
        reloaded.LineItems.ShouldBeEmpty();
    }

    [Fact]
    public async Task Create_FreeItemWithPriceZero_SavesCorrectly()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        InvoiceFormDto form = InvoiceTestHelper.CreateValidForm(customerId);
        List<LineItemFormDto> lineItems =
        [
            new(0, "Free Sample", 1m, 0m, 0m),
        ];

        // Act
        Result<int> result = await helper.CommandService.SaveAsync(0, form, lineItems);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Invoice? reloaded = await helper.ReloadInvoiceAsync(result.Value);
        reloaded.ShouldNotBeNull();

        LineItem freeItem = reloaded.LineItems.Single();
        freeItem.Description.ShouldBe("Free Sample");
        freeItem.UnitPrice.ShouldBe(0m);
        freeItem.LineTotal().ShouldBe(0m);
    }

    [Fact]
    public async Task Create_LineItemWith100PercentDiscount_SavesWithZeroLineTotal()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        InvoiceFormDto form = InvoiceTestHelper.CreateValidForm(customerId);
        List<LineItemFormDto> lineItems =
        [
            new(0, "100% Discounted", 2m, 100m, 100m),
        ];

        // Act
        Result<int> result = await helper.CommandService.SaveAsync(0, form, lineItems);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Invoice? reloaded = await helper.ReloadInvoiceAsync(result.Value);
        reloaded.ShouldNotBeNull();

        LineItem discountedItem = reloaded.LineItems.Single();
        discountedItem.DiscountPercent.ShouldBe(100m);
        discountedItem.LineTotal().ShouldBe(0m);
    }

    [Fact]
    public async Task Create_InvalidCustomerId_ReturnsFailure_NothingInDatabase()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();

        InvoiceFormDto form = new(0, FixedToday, FixedToday.AddDays(30), 10m); // Invalid: CustomerId = 0
        List<LineItemFormDto> lineItems = [InvoiceTestHelper.CreateValidLineItem()];

        // Act
        Result<int> result = await helper.CommandService.SaveAsync(0, form, lineItems);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Customer is required");

        int invoiceCount = await helper.CountInvoicesAsync();
        invoiceCount.ShouldBe(0);
    }

    [Fact]
    public async Task Create_DueDateBeforeIssueDate_ReturnsFailure_NothingInDatabase()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        InvoiceFormDto form = new(customerId, FixedToday, FixedToday.AddDays(-1), 10m);
        List<LineItemFormDto> lineItems = [InvoiceTestHelper.CreateValidLineItem()];

        // Act
        Result<int> result = await helper.CommandService.SaveAsync(0, form, lineItems);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Due date must be on or after the issue date");

        int invoiceCount = await helper.CountInvoicesAsync();
        invoiceCount.ShouldBe(0);
    }

    [Fact]
    public async Task Create_TaxRateOver100_ReturnsFailure_NothingInDatabase()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        InvoiceFormDto form = new(customerId, FixedToday, FixedToday.AddDays(30), 101m);
        List<LineItemFormDto> lineItems = [InvoiceTestHelper.CreateValidLineItem()];

        // Act
        Result<int> result = await helper.CommandService.SaveAsync(0, form, lineItems);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Tax rate must be between 0 and 100");

        int invoiceCount = await helper.CountInvoicesAsync();
        invoiceCount.ShouldBe(0);
    }

    [Fact]
    public async Task Create_OneInvalidLineAmongValid_EmptyDescription_ReturnsFailure_NothingSaved()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        InvoiceFormDto form = InvoiceTestHelper.CreateValidForm(customerId);
        List<LineItemFormDto> lineItems =
        [
            new(0, "Valid Item", 1m, 100m, 0m),
            new(0, "", 1m, 50m, 0m), // Invalid: empty description
            new(0, "Another Valid", 2m, 75m, 5m),
        ];

        // Act
        Result<int> result = await helper.CommandService.SaveAsync(0, form, lineItems);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Description is required");

        int invoiceCount = await helper.CountInvoicesAsync();
        int lineItemCount = await helper.CountLineItemsAsync();
        invoiceCount.ShouldBe(0);
        lineItemCount.ShouldBe(0);
    }

    [Fact]
    public async Task Create_OneInvalidLine_QuantityZero_ReturnsFailure_NothingSaved()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        InvoiceFormDto form = InvoiceTestHelper.CreateValidForm(customerId);
        List<LineItemFormDto> lineItems =
        [
            new(0, "Valid Item", 1m, 100m, 0m),
            new(0, "Zero Quantity", 0m, 50m, 0m), // Invalid: quantity = 0
        ];

        // Act
        Result<int> result = await helper.CommandService.SaveAsync(0, form, lineItems);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Quantity must be greater than 0");

        int invoiceCount = await helper.CountInvoicesAsync();
        int lineItemCount = await helper.CountLineItemsAsync();
        invoiceCount.ShouldBe(0);
        lineItemCount.ShouldBe(0);
    }

    [Fact]
    public async Task Create_OneInvalidLine_DiscountOver100_ReturnsFailure_NothingSaved()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        InvoiceFormDto form = InvoiceTestHelper.CreateValidForm(customerId);
        List<LineItemFormDto> lineItems =
        [
            new(0, "Valid Item", 1m, 100m, 0m),
            new(0, "Over Discounted", 1m, 50m, 101m), // Invalid: discount > 100
        ];

        // Act
        Result<int> result = await helper.CommandService.SaveAsync(0, form, lineItems);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Discount must be between 0 and 100");

        int invoiceCount = await helper.CountInvoicesAsync();
        int lineItemCount = await helper.CountLineItemsAsync();
        invoiceCount.ShouldBe(0);
        lineItemCount.ShouldBe(0);
    }

    [Fact]
    public async Task Create_DescriptionWithSurroundingSpaces_IsTrimmed()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        InvoiceFormDto form = InvoiceTestHelper.CreateValidForm(customerId);
        List<LineItemFormDto> lineItems =
        [
            new(0, "  Padded Description  ", 1m, 100m, 0m),
        ];

        // Act
        Result<int> result = await helper.CommandService.SaveAsync(0, form, lineItems);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Invoice? reloaded = await helper.ReloadInvoiceAsync(result.Value);
        reloaded.ShouldNotBeNull();

        LineItem item = reloaded.LineItems.Single();
        item.Description.ShouldBe("Padded Description");
    }

    [Fact]
    public async Task Create_DecimalsRoundTrip_ExactValues()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        decimal taxRate = 12.34m;
        InvoiceFormDto form = new(customerId, FixedToday, FixedToday.AddDays(30), taxRate);
        List<LineItemFormDto> lineItems =
        [
            new(0, "Precise Item", 1.23m, 45.67m, 8.9m),
        ];

        // Act
        Result<int> result = await helper.CommandService.SaveAsync(0, form, lineItems);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Invoice? reloaded = await helper.ReloadInvoiceAsync(result.Value);
        reloaded.ShouldNotBeNull();
        reloaded.TaxRate.ShouldBe(12.34m);

        LineItem item = reloaded.LineItems.Single();
        item.Quantity.ShouldBe(1.23m);
        item.UnitPrice.ShouldBe(45.67m);
        item.DiscountPercent.ShouldBe(8.9m);
    }

    [Fact]
    public async Task Create_DatesRoundTrip_ExactValues()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        DateOnly issueDate = new(2024, 12, 25);
        DateOnly dueDate = new(2025, 1, 15);
        InvoiceFormDto form = new(customerId, issueDate, dueDate, 10m);
        List<LineItemFormDto> lineItems = [InvoiceTestHelper.CreateValidLineItem()];

        // Act
        Result<int> result = await helper.CommandService.SaveAsync(0, form, lineItems);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Invoice? reloaded = await helper.ReloadInvoiceAsync(result.Value);
        reloaded.ShouldNotBeNull();
        reloaded.IssueDate.ShouldBe(issueDate);
        reloaded.DueDate.ShouldBe(dueDate);
    }
}
