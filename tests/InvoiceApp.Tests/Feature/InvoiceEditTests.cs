using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Tests.TestSupport;
using Shouldly;

namespace InvoiceApp.Tests.Feature;

public sealed class InvoiceEditTests
{
    private static readonly DateOnly FixedToday = InvoiceTestHelper.FixedToday;

    [Fact]
    public async Task Edit_ChangeHeaderOnly_HeaderUpdatedLinesUntouched()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customer1Id = await helper.SeedCustomerAsync("Customer 1", "Company 1");
        int customer2Id = await helper.SeedCustomerAsync("Customer 2", "Company 2");

        int invoiceId = await helper.SeedInvoiceAsync(
            customer1Id,
            taxRate: 10m,
            lineItems: [("Original Item", 1m, 100m, 0m)]
        );

        // Get the actual line item ID
        Invoice? before = await helper.ReloadInvoiceAsync(invoiceId);
        int lineItemId = before!.LineItems.Single().Id;

        // Act
        InvoiceFormDto newForm = new(
            customer2Id,
            FixedToday.AddDays(5),
            FixedToday.AddDays(40),
            25m
        );
        List<LineItemFormDto> sameLineItems = [new(lineItemId, "Original Item", 1m, 100m, 0m)];

        Result<int> result = await helper.CommandService.SaveAsync(
            invoiceId,
            newForm,
            sameLineItems
        );

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Invoice? reloaded = await helper.ReloadInvoiceAsync(invoiceId);
        reloaded.ShouldNotBeNull();
        reloaded.CustomerId.ShouldBe(customer2Id);
        reloaded.IssueDate.ShouldBe(FixedToday.AddDays(5));
        reloaded.DueDate.ShouldBe(FixedToday.AddDays(40));
        reloaded.TaxRate.ShouldBe(25m);

        reloaded.LineItems.Count.ShouldBe(1);
        LineItem item = reloaded.LineItems.Single();
        item.Description.ShouldBe("Original Item");
        item.Quantity.ShouldBe(1m);
        item.UnitPrice.ShouldBe(100m);
    }

    [Fact]
    public async Task Edit_AddUpdateRemoveLineInOneSave_ExactChangesApplied()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        int invoiceId = await helper.SeedInvoiceAsync(
            customerId,
            lineItems:
            [
                ("Item To Keep", 1m, 100m, 0m), // Will update
                ("Item To Remove", 2m, 50m, 0m), // Will remove
            ]
        );

        Invoice? before = await helper.ReloadInvoiceAsync(invoiceId);
        int keepItemId = before!.LineItems.Single(l => l.Description == "Item To Keep").Id;
        int removeItemId = before.LineItems.Single(l => l.Description == "Item To Remove").Id;

        // Act
        InvoiceFormDto form = InvoiceTestHelper.CreateValidForm(customerId);
        List<LineItemFormDto> newLineItems =
        [
            new(keepItemId, "Item To Keep - Updated", 3m, 150m, 10m), // Update existing
            new(0, "Brand New Item", 2m, 200m, 5m), // Add new
            // removeItemId is NOT in the list -> it will be removed
        ];

        Result<int> result = await helper.CommandService.SaveAsync(invoiceId, form, newLineItems);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Invoice? reloaded = await helper.ReloadInvoiceAsync(invoiceId);
        reloaded.ShouldNotBeNull();
        reloaded.LineItems.Count.ShouldBe(2);

        // Verify updated item
        LineItem updatedItem = reloaded.LineItems.Single(l => l.Id == keepItemId);
        updatedItem.Description.ShouldBe("Item To Keep - Updated");
        updatedItem.Quantity.ShouldBe(3m);
        updatedItem.UnitPrice.ShouldBe(150m);
        updatedItem.DiscountPercent.ShouldBe(10m);

        // Verify new item
        LineItem newItem = reloaded.LineItems.Single(l => l.Description == "Brand New Item");
        newItem.Id.ShouldNotBe(0);
        newItem.Quantity.ShouldBe(2m);
        newItem.UnitPrice.ShouldBe(200m);

        // Verify removed item is gone
        reloaded.LineItems.ShouldNotContain(l => l.Id == removeItemId);
    }

    [Fact]
    public async Task Edit_RemoveAllLines_ReturnsFailure()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        int invoiceId = await helper.SeedInvoiceAsync(
            customerId,
            lineItems: [("Line 1", 1m, 100m, 0m), ("Line 2", 2m, 50m, 5m)]
        );

        // Act
        InvoiceFormDto form = InvoiceTestHelper.CreateValidForm(customerId);
        List<LineItemFormDto> emptyLines = [];

        Result<int> result = await helper.CommandService.SaveAsync(invoiceId, form, emptyLines);

        // Assert - Cannot save invoice with no line items
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("at least one line item");
    }

    [Fact]
    public async Task Edit_SaveWithNoChanges_DataIdentical()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        int invoiceId = await helper.SeedInvoiceAsync(
            customerId,
            issueDate: FixedToday,
            dueDate: FixedToday.AddDays(30),
            taxRate: 15m,
            lineItems: [("Original", 2m, 100m, 10m)]
        );

        Invoice? before = await helper.ReloadInvoiceAsync(invoiceId);
        int lineItemId = before!.LineItems.Single().Id;

        // Act
        InvoiceFormDto sameForm = new(customerId, FixedToday, FixedToday.AddDays(30), 15m);
        List<LineItemFormDto> sameLines = [new(lineItemId, "Original", 2m, 100m, 10m)];

        Result<int> result = await helper.CommandService.SaveAsync(invoiceId, sameForm, sameLines);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Invoice? after = await helper.ReloadInvoiceAsync(invoiceId);
        after.ShouldNotBeNull();
        after.CustomerId.ShouldBe(customerId);
        after.IssueDate.ShouldBe(FixedToday);
        after.DueDate.ShouldBe(FixedToday.AddDays(30));
        after.TaxRate.ShouldBe(15m);

        LineItem line = after.LineItems.Single();
        line.Description.ShouldBe("Original");
        line.Quantity.ShouldBe(2m);
        line.UnitPrice.ShouldBe(100m);
        line.DiscountPercent.ShouldBe(10m);
    }

    [Fact]
    public async Task Edit_ChangeTaxRate_TotalRecalculatesCorrectly()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        int invoiceId = await helper.SeedInvoiceAsync(
            customerId,
            taxRate: 10m,
            lineItems: [("Item", 1m, 100m, 0m)] // Subtotal = 100
        );

        Invoice? before = await helper.ReloadInvoiceAsync(invoiceId);
        int lineItemId = before!.LineItems.Single().Id;

        // Act
        InvoiceFormDto newForm = new(customerId, FixedToday, FixedToday.AddDays(30), 20m);
        List<LineItemFormDto> sameLines = [new(lineItemId, "Item", 1m, 100m, 0m)];

        Result<int> result = await helper.CommandService.SaveAsync(invoiceId, newForm, sameLines);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Invoice? reloaded = await helper.ReloadInvoiceAsync(invoiceId);
        reloaded.ShouldNotBeNull();
        reloaded.TaxRate.ShouldBe(20m);

        decimal expectedTotal = InvoiceCalculations.Total(reloaded, reloaded.LineItems);
        expectedTotal.ShouldBe(120m); // 100 + 20% = 120

        InvoiceDetailsDto? details = await helper.QueryService.GetDetailsAsync(invoiceId);
        details.ShouldNotBeNull();
        details.Total.ShouldBe(expectedTotal);
    }

    [Fact]
    public async Task Edit_LineIdBelongsToAnotherInvoice_Ignored_OtherInvoiceNotModified()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        // Create two invoices
        int invoice1Id = await helper.SeedInvoiceAsync(
            customerId,
            lineItems: [("Invoice1 Item", 1m, 100m, 0m)]
        );
        int invoice2Id = await helper.SeedInvoiceAsync(
            customerId,
            lineItems: [("Invoice2 Item", 2m, 200m, 5m)]
        );

        Invoice? invoice2Before = await helper.ReloadInvoiceAsync(invoice2Id);
        int invoice2LineId = invoice2Before!.LineItems.Single().Id;

        // Act - Try to include invoice2's line item in invoice1's save
        InvoiceFormDto form = InvoiceTestHelper.CreateValidForm(customerId);
        List<LineItemFormDto> linesWithForeignId =
        [
            new(invoice2LineId, "Hijacked Item", 5m, 500m, 0m), // This ID belongs to invoice2
        ];

        Result<int> result = await helper.CommandService.SaveAsync(
            invoice1Id,
            form,
            linesWithForeignId
        );

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Invoice1 should NOT have the item with invoice2's line ID
        Invoice? invoice1After = await helper.ReloadInvoiceAsync(invoice1Id);
        invoice1After.ShouldNotBeNull();
        invoice1After.LineItems.ShouldNotContain(l => l.Id == invoice2LineId);

        // Invoice2 should be unchanged
        Invoice? invoice2After = await helper.ReloadInvoiceAsync(invoice2Id);
        invoice2After.ShouldNotBeNull();
        invoice2After.LineItems.Count.ShouldBe(1);
        LineItem invoice2Line = invoice2After.LineItems.Single();
        invoice2Line.Id.ShouldBe(invoice2LineId);
        invoice2Line.Description.ShouldBe("Invoice2 Item");
        invoice2Line.Quantity.ShouldBe(2m);
        invoice2Line.UnitPrice.ShouldBe(200m);
    }

    [Theory]
    [InlineData(InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    public async Task Edit_NonDraftInvoice_ReturnsFailure_NothingChanged(InvoiceStatus status)
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        // Sent and Paid need at least one line item
        List<(string, decimal, decimal, decimal)>? lineItems =
            status == InvoiceStatus.Cancelled ? null : [("Item", 1m, 100m, 0m)];

        int invoiceId = await helper.SeedInvoiceAsync(
            customerId,
            status: status,
            taxRate: 10m,
            lineItems: lineItems
        );

        // Act
        InvoiceFormDto newForm = new(
            customerId,
            FixedToday.AddDays(1),
            FixedToday.AddDays(31),
            50m
        );
        List<LineItemFormDto> newLines = [new(0, "New Item", 5m, 500m, 0m)];

        Result<int> result = await helper.CommandService.SaveAsync(invoiceId, newForm, newLines);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Only draft invoices can be edited");

        Invoice? reloaded = await helper.ReloadInvoiceAsync(invoiceId);
        reloaded.ShouldNotBeNull();
        reloaded.TaxRate.ShouldBe(10m);
        reloaded.Status.ShouldBe(status);
    }

    [Fact]
    public async Task Edit_MissingInvoice_ReturnsNotFound()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        InvoiceFormDto form = InvoiceTestHelper.CreateValidForm(customerId);
        List<LineItemFormDto> lines = [InvoiceTestHelper.CreateValidLineItem()];

        // Act
        Result<int> result = await helper.CommandService.SaveAsync(9999, form, lines);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("not found");
    }

    [Fact]
    public async Task Edit_InvoiceChangedToSentAfterLoad_ReturnsFailure()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        int invoiceId = await helper.SeedInvoiceAsync(
            customerId,
            status: InvoiceStatus.Draft,
            lineItems: [("Item", 1m, 100m, 0m)]
        );

        // Simulate: User loaded the invoice as Draft
        Invoice? loadedForEdit = await helper.ReloadInvoiceAsync(invoiceId);
        loadedForEdit.ShouldNotBeNull();
        loadedForEdit.Status.ShouldBe(InvoiceStatus.Draft);
        int lineItemId = loadedForEdit.LineItems.Single().Id;

        // Meanwhile, another user changes it to Sent
        await helper.CommandService.ChangeStatusAsync(invoiceId, InvoiceStatus.Sent);

        // Original user tries to save their edits
        InvoiceFormDto form = new(customerId, FixedToday, FixedToday.AddDays(30), 20m);
        List<LineItemFormDto> lines = [new(lineItemId, "Modified Item", 2m, 150m, 5m)];

        // Act
        Result<int> result = await helper.CommandService.SaveAsync(invoiceId, form, lines);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Only draft invoices can be edited");
    }

    [Fact]
    public async Task Edit_AddMultipleNewLines_AllSaved()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        int invoiceId = await helper.SeedInvoiceAsync(customerId);

        // Act
        InvoiceFormDto form = InvoiceTestHelper.CreateValidForm(customerId);
        List<LineItemFormDto> newLines =
        [
            new(0, "New Item 1", 1m, 100m, 0m),
            new(0, "New Item 2", 2m, 200m, 5m),
            new(0, "New Item 3", 3m, 300m, 10m),
        ];

        Result<int> result = await helper.CommandService.SaveAsync(invoiceId, form, newLines);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Invoice? reloaded = await helper.ReloadInvoiceAsync(invoiceId);
        reloaded.ShouldNotBeNull();
        reloaded.LineItems.Count.ShouldBe(3);

        reloaded.LineItems.ShouldContain(l => l.Description == "New Item 1");
        reloaded.LineItems.ShouldContain(l => l.Description == "New Item 2");
        reloaded.LineItems.ShouldContain(l => l.Description == "New Item 3");
    }

    [Fact]
    public async Task Edit_UpdateLineWithInvalidData_ReturnsFailure_NothingChanged()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        int invoiceId = await helper.SeedInvoiceAsync(
            customerId,
            lineItems: [("Original Item", 1m, 100m, 0m)]
        );

        Invoice? before = await helper.ReloadInvoiceAsync(invoiceId);
        int lineItemId = before!.LineItems.Single().Id;

        // Act - Try to update with invalid quantity
        InvoiceFormDto form = InvoiceTestHelper.CreateValidForm(customerId);
        List<LineItemFormDto> invalidLines =
        [
            new(lineItemId, "Updated Item", 0m, 150m, 0m), // Invalid: quantity = 0
        ];

        Result<int> result = await helper.CommandService.SaveAsync(invoiceId, form, invalidLines);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Quantity must be greater than 0");

        Invoice? after = await helper.ReloadInvoiceAsync(invoiceId);
        after.ShouldNotBeNull();
        LineItem line = after.LineItems.Single();
        line.Description.ShouldBe("Original Item");
        line.Quantity.ShouldBe(1m);
    }
}
