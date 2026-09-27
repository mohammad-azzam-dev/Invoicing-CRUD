using InvoiceApp.Data;
using InvoiceApp.Domain;
using InvoiceApp.Tests.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace InvoiceApp.Tests.Feature;

public sealed class InvoiceSchemaTests
{
    [Fact]
    public async Task MigrationApplies_OnFreshDatabase()
    {
        // Arrange - use a fresh connection with MigrateAsync instead of EnsureCreatedAsync
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var db = new AppDbContext(options);

        // Act
        await db.Database.MigrateAsync();

        // Assert - tables exist and can be queried
        var invoiceCount = await db.Invoices.CountAsync();
        var lineItemCount = await db.LineItems.CountAsync();
        var customerCount = await db.Customers.CountAsync();

        invoiceCount.ShouldBe(0);
        lineItemCount.ShouldBe(0);
        customerCount.ShouldBe(0);
    }

    [Fact]
    public async Task SeederCreatesInvoicesAndLineItems()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        await using var db = await testDb.Factory.CreateDbContextAsync();

        // Create a customer first
        var customer = Customer.Create("Test", "555-0001", "test@test.com", "Test Customer");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var invoice = Invoice.Create(customer.Id, today, today.AddDays(30), 21m).Value!;
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var lineItem = LineItem.Create(invoice.Id, "Test Item", 1m, 100m, 0m);
        db.LineItems.Add(lineItem);
        await db.SaveChangesAsync();

        // Assert
        var invoices = await db.Invoices.ToListAsync();
        var lineItems = await db.LineItems.ToListAsync();

        invoices.ShouldNotBeEmpty();
        lineItems.ShouldNotBeEmpty();
        lineItems.First().InvoiceId.ShouldBe(invoice.Id);
    }

    [Fact]
    public async Task InvoicesCanBeQueriedByStatus()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        await using var db = await testDb.Factory.CreateDbContextAsync();

        // Create customers
        var draftCustomer = Customer.Create(
            "Draft",
            "555-0001",
            "draft@test.com",
            "Draft Customer"
        );
        var sentCustomer = Customer.Create("Sent", "555-0002", "sent@test.com", "Sent Customer");
        var paidCustomer = Customer.Create("Paid", "555-0003", "paid@test.com", "Paid Customer");
        var cancelledCustomer = Customer.Create(
            "Cancelled",
            "555-0004",
            "cancelled@test.com",
            "Cancelled Customer"
        );
        db.Customers.AddRange(draftCustomer, sentCustomer, paidCustomer, cancelledCustomer);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Create invoices with different statuses
        var draft = Invoice.Create(draftCustomer.Id, today, today.AddDays(30), 21m).Value!;
        db.Invoices.Add(draft);

        var sent = Invoice.Create(sentCustomer.Id, today, today.AddDays(30), 21m).Value!;
        sent.MarkAsSent(1);
        db.Invoices.Add(sent);

        var paid = Invoice.Create(paidCustomer.Id, today, today.AddDays(30), 21m).Value!;
        paid.MarkAsSent(1);
        paid.MarkAsPaid();
        db.Invoices.Add(paid);

        var cancelled = Invoice.Create(cancelledCustomer.Id, today, today.AddDays(30), 21m).Value!;
        cancelled.Cancel();
        db.Invoices.Add(cancelled);

        await db.SaveChangesAsync();

        // Act
        var draftInvoices = await db
            .Invoices.Where(i => i.Status == InvoiceStatus.Draft)
            .ToListAsync();
        var sentInvoices = await db
            .Invoices.Where(i => i.Status == InvoiceStatus.Sent)
            .ToListAsync();
        var paidInvoices = await db
            .Invoices.Where(i => i.Status == InvoiceStatus.Paid)
            .ToListAsync();
        var cancelledInvoices = await db
            .Invoices.Where(i => i.Status == InvoiceStatus.Cancelled)
            .ToListAsync();

        // Assert
        draftInvoices.Count.ShouldBe(1);
        sentInvoices.Count.ShouldBe(1);
        paidInvoices.Count.ShouldBe(1);
        cancelledInvoices.Count.ShouldBe(1);
    }

    [Fact]
    public async Task LineItemsAreCascadeDeletedWithInvoice()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        await using var db = await testDb.Factory.CreateDbContextAsync();

        var customer = Customer.Create("Test", "555-0001", "test@test.com", "Test Customer");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var invoice = Invoice.Create(customer.Id, today, today.AddDays(30), 21m).Value!;
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var lineItem1 = LineItem.Create(invoice.Id, "Item 1", 1m, 100m, 0m);
        var lineItem2 = LineItem.Create(invoice.Id, "Item 2", 2m, 50m, 10m);
        db.LineItems.AddRange(lineItem1, lineItem2);
        await db.SaveChangesAsync();

        var lineItemCountBefore = await db.LineItems.CountAsync();
        lineItemCountBefore.ShouldBe(2);

        // Act
        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync();

        // Assert
        var lineItemCountAfter = await db.LineItems.CountAsync();
        lineItemCountAfter.ShouldBe(0);
    }

    [Fact]
    public async Task DecimalFieldsRoundTripCorrectly()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        await using var db = await testDb.Factory.CreateDbContextAsync();

        var customer = Customer.Create("Test", "555-0001", "test@test.com", "Test Customer");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var invoice = Invoice.Create(customer.Id, today, today.AddDays(30), 21.5m).Value!;
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var lineItem = LineItem.Create(invoice.Id, "Test Item", 2.75m, 99.99m, 12.5m);
        db.LineItems.Add(lineItem);
        await db.SaveChangesAsync();

        // Act - fetch fresh from database
        await using var db2 = await testDb.Factory.CreateDbContextAsync();
        var loadedInvoice = await db2.Invoices.FirstAsync();
        var loadedLineItem = await db2.LineItems.FirstAsync();

        // Assert
        loadedInvoice.TaxRate.ShouldBe(21.5m);
        loadedLineItem.Quantity.ShouldBe(2.75m);
        loadedLineItem.UnitPrice.ShouldBe(99.99m);
        loadedLineItem.DiscountPercent.ShouldBe(12.5m);
    }

    [Fact]
    public async Task OverdueInvoicesCanBeIdentified()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        await using var db = await testDb.Factory.CreateDbContextAsync();

        var overdueCustomer = Customer.Create(
            "Test",
            "555-0001",
            "overdue@test.com",
            "Overdue Customer"
        );
        var notOverdueCustomer = Customer.Create(
            "Test",
            "555-0002",
            "notoverdue@test.com",
            "Not Overdue Customer"
        );
        db.Customers.AddRange(overdueCustomer, notOverdueCustomer);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Create an overdue sent invoice
        var overdueInvoice = Invoice
            .Create(overdueCustomer.Id, today.AddDays(-30), today.AddDays(-5), 21m)
            .Value!;
        overdueInvoice.MarkAsSent(1);
        db.Invoices.Add(overdueInvoice);

        // Create a non-overdue sent invoice
        var notOverdueInvoice = Invoice
            .Create(notOverdueCustomer.Id, today, today.AddDays(30), 21m)
            .Value!;
        notOverdueInvoice.MarkAsSent(1);
        db.Invoices.Add(notOverdueInvoice);

        await db.SaveChangesAsync();

        // Act
        var sentInvoices = await db
            .Invoices.Where(i => i.Status == InvoiceStatus.Sent)
            .ToListAsync();
        var overdueInvoices = sentInvoices
            .Where(i => InvoiceCalculations.IsOverdue(i, today))
            .ToList();

        // Assert
        overdueInvoices.Count.ShouldBe(1);
        overdueInvoices.First().CustomerId.ShouldBe(overdueCustomer.Id);
    }

    [Fact]
    public async Task DateOnlyFieldsRoundTripCorrectly()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        await using var db = await testDb.Factory.CreateDbContextAsync();

        var customer = Customer.Create("Test", "555-0001", "test@test.com", "Test Customer");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var issueDate = new DateOnly(2024, 6, 15);
        var dueDate = new DateOnly(2024, 7, 15);

        var invoice = Invoice.Create(customer.Id, issueDate, dueDate, 21m).Value!;
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        // Act - fetch fresh from database
        await using var db2 = await testDb.Factory.CreateDbContextAsync();
        var loadedInvoice = await db2.Invoices.FirstAsync();

        // Assert
        loadedInvoice.IssueDate.ShouldBe(issueDate);
        loadedInvoice.DueDate.ShouldBe(dueDate);
    }
}
