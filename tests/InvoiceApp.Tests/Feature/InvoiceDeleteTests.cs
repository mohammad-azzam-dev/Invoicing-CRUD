using InvoiceApp.Domain;
using InvoiceApp.Tests.TestSupport;
using Shouldly;

namespace InvoiceApp.Tests.Feature;

public sealed class InvoiceDeleteTests
{
    [Fact]
    public async Task Delete_DraftInvoice_RemovesFromDatabase()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        int invoiceId = await helper.SeedInvoiceAsync(
            customerId,
            status: InvoiceStatus.Draft,
            lineItems: [("Item 1", 1m, 100m, 0m), ("Item 2", 2m, 50m, 5m)]
        );

        int invoiceCountBefore = await helper.CountInvoicesAsync();
        int lineItemCountBefore = await helper.CountLineItemsAsync();
        invoiceCountBefore.ShouldBe(1);
        lineItemCountBefore.ShouldBe(2);

        // Act
        Result result = await helper.CommandService.DeleteAsync(invoiceId);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Invoice? reloaded = await helper.ReloadInvoiceAsync(invoiceId);
        reloaded.ShouldBeNull();

        int invoiceCountAfter = await helper.CountInvoicesAsync();
        int lineItemCountAfter = await helper.CountLineItemsAsync();
        invoiceCountAfter.ShouldBe(0);
        lineItemCountAfter.ShouldBe(0);
    }

    [Fact]
    public async Task Delete_DraftInvoiceWithNoLines_RemovesFromDatabase()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        int invoiceId = await helper.SeedInvoiceAsync(customerId, status: InvoiceStatus.Draft);

        // Act
        Result result = await helper.CommandService.DeleteAsync(invoiceId);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Invoice? reloaded = await helper.ReloadInvoiceAsync(invoiceId);
        reloaded.ShouldBeNull();
    }

    [Theory]
    [InlineData(InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    public async Task Delete_NonDraftInvoice_ReturnsFailure_NothingDeleted(InvoiceStatus status)
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
            lineItems: lineItems
        );

        // Act
        Result result = await helper.CommandService.DeleteAsync(invoiceId);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Only draft invoices can be deleted");

        Invoice? reloaded = await helper.ReloadInvoiceAsync(invoiceId);
        reloaded.ShouldNotBeNull();
        reloaded.Status.ShouldBe(status);
    }

    [Fact]
    public async Task Delete_MissingInvoice_ReturnsNotFound()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();

        // Act
        Result result = await helper.CommandService.DeleteAsync(9999);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("not found");
    }

    [Fact]
    public async Task Delete_CascadeDeletesLineItems_OtherInvoicesUnaffected()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        int invoiceToDelete = await helper.SeedInvoiceAsync(
            customerId,
            lineItems: [("Delete Me 1", 1m, 100m, 0m), ("Delete Me 2", 2m, 50m, 0m)]
        );

        int invoiceToKeep = await helper.SeedInvoiceAsync(
            customerId,
            lineItems: [("Keep Me 1", 3m, 75m, 10m), ("Keep Me 2", 1m, 200m, 0m)]
        );

        int totalLinesBefore = await helper.CountLineItemsAsync();
        totalLinesBefore.ShouldBe(4);

        // Act
        Result result = await helper.CommandService.DeleteAsync(invoiceToDelete);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Invoice? deletedInvoice = await helper.ReloadInvoiceAsync(invoiceToDelete);
        deletedInvoice.ShouldBeNull();

        Invoice? keptInvoice = await helper.ReloadInvoiceAsync(invoiceToKeep);
        keptInvoice.ShouldNotBeNull();
        keptInvoice.LineItems.Count.ShouldBe(2);
        keptInvoice.LineItems.ShouldContain(l => l.Description == "Keep Me 1");
        keptInvoice.LineItems.ShouldContain(l => l.Description == "Keep Me 2");

        int totalLinesAfter = await helper.CountLineItemsAsync();
        totalLinesAfter.ShouldBe(2);
    }

    [Fact]
    public async Task Delete_MultipleDraftInvoices_DeletesCorrectOne()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        int invoice1 = await helper.SeedInvoiceAsync(customerId, lineItems: [("Item 1", 1m, 100m, 0m)]);
        int invoice2 = await helper.SeedInvoiceAsync(customerId, lineItems: [("Item 2", 2m, 200m, 5m)]);
        int invoice3 = await helper.SeedInvoiceAsync(customerId, lineItems: [("Item 3", 3m, 300m, 10m)]);

        // Act - Delete the middle one
        Result result = await helper.CommandService.DeleteAsync(invoice2);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Invoice? deleted = await helper.ReloadInvoiceAsync(invoice2);
        deleted.ShouldBeNull();

        Invoice? kept1 = await helper.ReloadInvoiceAsync(invoice1);
        Invoice? kept3 = await helper.ReloadInvoiceAsync(invoice3);

        kept1.ShouldNotBeNull();
        kept1.LineItems.Single().Description.ShouldBe("Item 1");

        kept3.ShouldNotBeNull();
        kept3.LineItems.Single().Description.ShouldBe("Item 3");

        int invoiceCount = await helper.CountInvoicesAsync();
        invoiceCount.ShouldBe(2);
    }

    [Fact]
    public async Task Delete_InvoiceChangedToSentAfterLoad_ReturnsFailure()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        int invoiceId = await helper.SeedInvoiceAsync(
            customerId,
            status: InvoiceStatus.Draft,
            lineItems: [("Item", 1m, 100m, 0m)]
        );

        // Simulate: User sees the invoice as Draft in the UI
        Invoice? loadedByUser = await helper.ReloadInvoiceAsync(invoiceId);
        loadedByUser.ShouldNotBeNull();
        loadedByUser.Status.ShouldBe(InvoiceStatus.Draft);

        // Meanwhile, another user marks it as Sent
        await helper.CommandService.ChangeStatusAsync(invoiceId, InvoiceStatus.Sent);

        // Original user tries to delete
        // Act
        Result result = await helper.CommandService.DeleteAsync(invoiceId);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Only draft invoices can be deleted");

        Invoice? stillExists = await helper.ReloadInvoiceAsync(invoiceId);
        stillExists.ShouldNotBeNull();
        stillExists.Status.ShouldBe(InvoiceStatus.Sent);
    }

    [Fact]
    public async Task Delete_SameInvoiceTwice_SecondAttemptReturnsNotFound()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        int invoiceId = await helper.SeedInvoiceAsync(customerId);

        // Act
        Result firstDelete = await helper.CommandService.DeleteAsync(invoiceId);
        Result secondDelete = await helper.CommandService.DeleteAsync(invoiceId);

        // Assert
        firstDelete.IsSuccess.ShouldBeTrue();
        secondDelete.IsSuccess.ShouldBeFalse();
        secondDelete.Error!.ShouldContain("not found");
    }

    [Fact]
    public async Task Delete_CustomerStillExistsAfterInvoiceDeleted()
    {
        // Arrange
        await using InvoiceTestHelper helper = await InvoiceTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync("Test Customer", "Test Company");

        int invoiceId = await helper.SeedInvoiceAsync(customerId);

        // Act
        Result result = await helper.CommandService.DeleteAsync(invoiceId);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Customer should still exist (not cascade deleted)
        await using var db = await helper.DbFactory.CreateDbContextAsync();
        Customer? customer = await db.Customers.FindAsync(customerId);
        customer.ShouldNotBeNull();
        customer.Name.ShouldBe("Test Customer");
    }
}
