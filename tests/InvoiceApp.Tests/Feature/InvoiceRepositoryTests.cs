using InvoiceApp.Data;
using InvoiceApp.Data.Repositories;
using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Tests.TestSupport;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace InvoiceApp.Tests.Feature;

public sealed class InvoiceRepositoryTests
{
    private static readonly DateOnly FixedToday = new(2024, 6, 15);

    [Fact]
    public async Task Search_ByCustomerName_ReturnsMatchingInvoices()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );
        var repository = new InvoiceRepository(testDb.Factory, timeProvider);

        await SeedInvoicesAsync(testDb);

        var query = InvoiceQuery.Default with { Search = "Acme" };

        // Act
        var result = await repository.GetPagedAsync(query);

        // Assert
        result.Items.ShouldNotBeEmpty();
        result.Items.ShouldAllBe(i =>
            i.CustomerName.Contains("Acme", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public async Task Search_ByInvoiceNumber_ReturnsMatchingInvoice()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );
        var repository = new InvoiceRepository(testDb.Factory, timeProvider);

        await SeedInvoicesAsync(testDb);

        var query = InvoiceQuery.Default with { Search = "INV-00001" };

        // Act
        var result = await repository.GetPagedAsync(query);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].Id.ShouldBe(1);
    }

    [Fact]
    public async Task Filter_ByStatus_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );
        var repository = new InvoiceRepository(testDb.Factory, timeProvider);

        await SeedInvoicesAsync(testDb);

        var query = InvoiceQuery.Default with { Status = InvoiceStatus.Sent };

        // Act
        var result = await repository.GetPagedAsync(query);

        // Assert
        result.Items.ShouldNotBeEmpty();
        result.Items.ShouldAllBe(i => i.Status == InvoiceStatus.Sent);
    }

    [Theory]
    [InlineData(InvoiceSortField.Number, false)]
    [InlineData(InvoiceSortField.Number, true)]
    [InlineData(InvoiceSortField.CustomerName, false)]
    [InlineData(InvoiceSortField.CustomerName, true)]
    [InlineData(InvoiceSortField.IssueDate, false)]
    [InlineData(InvoiceSortField.IssueDate, true)]
    [InlineData(InvoiceSortField.DueDate, false)]
    [InlineData(InvoiceSortField.DueDate, true)]
    [InlineData(InvoiceSortField.ItemCount, false)]
    [InlineData(InvoiceSortField.ItemCount, true)]
    public async Task Sort_AllFields_ReturnsSortedResults(InvoiceSortField sortBy, bool descending)
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );
        var repository = new InvoiceRepository(testDb.Factory, timeProvider);

        await SeedInvoicesAsync(testDb);

        var query = InvoiceQuery.Default with
        {
            SortBy = sortBy,
            Descending = descending,
            PageSize = 100,
        };

        // Act
        var result = await repository.GetPagedAsync(query);

        // Assert
        result.Items.Count.ShouldBeGreaterThan(1);

        // Verify sorting - primary field follows descending flag, ties break by Id ascending
        var items = result.Items.ToList();
        for (var i = 1; i < items.Count; i++)
        {
            var (primary, id) = CompareByField(items[i - 1], items[i], sortBy);

            if (descending)
            {
                // Primary sort descending: item[i-1] >= item[i]
                // Ties break by Id ascending: item[i-1].Id < item[i].Id
                if (primary == 0)
                {
                    id.ShouldBeLessThanOrEqualTo(
                        0,
                        $"Sort by {sortBy} descending - tie should break by Id ascending at index {i}"
                    );
                }
                else
                {
                    primary.ShouldBeGreaterThanOrEqualTo(
                        0,
                        $"Sort by {sortBy} descending failed at index {i}"
                    );
                }
            }
            else
            {
                // Primary sort ascending: item[i-1] <= item[i]
                // Ties break by Id ascending: item[i-1].Id < item[i].Id
                if (primary == 0)
                {
                    id.ShouldBeLessThanOrEqualTo(
                        0,
                        $"Sort by {sortBy} ascending - tie should break by Id ascending at index {i}"
                    );
                }
                else
                {
                    primary.ShouldBeLessThanOrEqualTo(
                        0,
                        $"Sort by {sortBy} ascending failed at index {i}"
                    );
                }
            }
        }
    }

    [Fact]
    public async Task Sort_ByTotal_MatchesDomainCalculation()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );
        var repository = new InvoiceRepository(testDb.Factory, timeProvider);

        await using var db = await testDb.Factory.CreateDbContextAsync();

        // Create customers first
        var customer = Customer.Create("Test", "555-0001", "test@test.com", "Test Corp");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        // Create invoices with specific totals
        var invoice1 = Invoice.Create(customer.Id, FixedToday, FixedToday.AddDays(30), 10m).Value!;
        var invoice2 = Invoice.Create(customer.Id, FixedToday, FixedToday.AddDays(30), 20m).Value!;
        var invoice3 = Invoice.Create(customer.Id, FixedToday, FixedToday.AddDays(30), 15m).Value!;

        db.Invoices.AddRange(invoice1, invoice2, invoice3);
        await db.SaveChangesAsync();

        // Add line items: invoice1 total ~110, invoice2 total ~240, invoice3 total ~172.5
        db.LineItems.AddRange(
            LineItem.Create(invoice1.Id, "Item", 1m, 100m, 0m), // 100 + 10% tax = 110
            LineItem.Create(invoice2.Id, "Item", 2m, 100m, 0m), // 200 + 20% tax = 240
            LineItem.Create(invoice3.Id, "Item", 1.5m, 100m, 0m) // 150 + 15% tax = 172.5
        );
        await db.SaveChangesAsync();

        var query = InvoiceQuery.Default with
        {
            SortBy = InvoiceSortField.Total,
            Descending = false,
            PageSize = 100,
        };

        // Act
        var result = await repository.GetPagedAsync(query);

        // Assert
        var totals = result.Items.Select(i => i.Total).ToList();
        totals.ShouldBe(
            totals.OrderBy(t => t).ToList(),
            "Total sort ascending should match Domain calculation order"
        );
    }

    [Fact]
    public async Task Sort_WithTies_UsesIdForStableOrder()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );
        var repository = new InvoiceRepository(testDb.Factory, timeProvider);

        await using var db = await testDb.Factory.CreateDbContextAsync();

        // Create customer - all invoices will show this customer's name
        var customer = Customer.Create("Same Name", "555-0001", "same@test.com", "Same Company");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        // Create invoices with same customer (same name)
        var invoice1 = Invoice.Create(customer.Id, FixedToday, FixedToday.AddDays(30), 10m).Value!;
        var invoice2 = Invoice.Create(customer.Id, FixedToday, FixedToday.AddDays(30), 10m).Value!;
        var invoice3 = Invoice.Create(customer.Id, FixedToday, FixedToday.AddDays(30), 10m).Value!;

        db.Invoices.AddRange(invoice1, invoice2, invoice3);
        await db.SaveChangesAsync();

        var query = InvoiceQuery.Default with
        {
            SortBy = InvoiceSortField.CustomerName,
            Descending = false,
            PageSize = 100,
        };

        // Act
        var result = await repository.GetPagedAsync(query);

        // Assert - IDs should be in ascending order when names are equal
        var ids = result.Items.Select(i => i.Id).ToList();
        ids.ShouldBe(
            ids.OrderBy(id => id).ToList(),
            "With same CustomerName, should be ordered by Id ascending"
        );
    }

    [Fact]
    public async Task Paging_ReturnsCorrectPageAndTotalCount()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );
        var repository = new InvoiceRepository(testDb.Factory, timeProvider);

        await using var db = await testDb.Factory.CreateDbContextAsync();

        // Create customer
        var customer = Customer.Create("Test", "555-0001", "test@test.com");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        // Create 15 invoices
        for (var i = 0; i < 15; i++)
        {
            var invoice = Invoice
                .Create(customer.Id, FixedToday, FixedToday.AddDays(30), 10m)
                .Value!;
            db.Invoices.Add(invoice);
        }
        await db.SaveChangesAsync();

        var query = InvoiceQuery.Default with
        {
            Page = 2,
            PageSize = 5,
            SortBy = InvoiceSortField.Number,
            Descending = false,
        };

        // Act
        var result = await repository.GetPagedAsync(query);

        // Assert
        result.TotalCount.ShouldBe(15);
        result.Items.Count.ShouldBe(5);
        result.Items[0].Id.ShouldBe(6); // Second page starts at ID 6 (1-indexed, 0-based skip of 5)
    }

    [Fact]
    public async Task IsOverdue_TrueForSentInvoicesWithPastDueDate()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );
        var repository = new InvoiceRepository(testDb.Factory, timeProvider);

        await using var db = await testDb.Factory.CreateDbContextAsync();

        // Create customers
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
        var draftCustomer = Customer.Create("Test", "555-0003", "draft@test.com", "Draft Customer");
        db.Customers.AddRange(overdueCustomer, notOverdueCustomer, draftCustomer);
        await db.SaveChangesAsync();

        // Create overdue sent invoice
        var overdueInvoice = Invoice
            .Create(overdueCustomer.Id, FixedToday.AddDays(-30), FixedToday.AddDays(-5), 10m)
            .Value!;
        overdueInvoice.MarkAsSent(1);
        db.Invoices.Add(overdueInvoice);

        // Create non-overdue sent invoice
        var notOverdueInvoice = Invoice
            .Create(notOverdueCustomer.Id, FixedToday, FixedToday.AddDays(30), 10m)
            .Value!;
        notOverdueInvoice.MarkAsSent(1);
        db.Invoices.Add(notOverdueInvoice);

        // Create overdue draft invoice (should NOT be marked overdue)
        var overdueDraft = Invoice
            .Create(draftCustomer.Id, FixedToday.AddDays(-30), FixedToday.AddDays(-5), 10m)
            .Value!;
        db.Invoices.Add(overdueDraft);

        await db.SaveChangesAsync();

        // Add line items
        db.LineItems.AddRange(
            LineItem.Create(overdueInvoice.Id, "Item", 1m, 100m, 0m),
            LineItem.Create(notOverdueInvoice.Id, "Item", 1m, 100m, 0m),
            LineItem.Create(overdueDraft.Id, "Item", 1m, 100m, 0m)
        );
        await db.SaveChangesAsync();

        var query = InvoiceQuery.Default with { PageSize = 100 };

        // Act
        var result = await repository.GetPagedAsync(query);

        // Assert
        var overdueItem = result.Items.Single(i => i.CustomerName == "Overdue Customer");
        overdueItem.IsOverdue.ShouldBeTrue();

        var notOverdueItem = result.Items.Single(i => i.CustomerName == "Not Overdue Customer");
        notOverdueItem.IsOverdue.ShouldBeFalse();

        var draftItem = result.Items.Single(i => i.CustomerName == "Draft Customer");
        draftItem.IsOverdue.ShouldBeFalse();
    }

    [Fact]
    public async Task ItemCount_ReturnsCorrectCount()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );
        var repository = new InvoiceRepository(testDb.Factory, timeProvider);

        await using var db = await testDb.Factory.CreateDbContextAsync();

        var customer = Customer.Create("Test", "555-0001", "test@test.com", "Test Customer");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var invoice = Invoice.Create(customer.Id, FixedToday, FixedToday.AddDays(30), 10m).Value!;
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        db.LineItems.AddRange(
            LineItem.Create(invoice.Id, "Item 1", 1m, 100m, 0m),
            LineItem.Create(invoice.Id, "Item 2", 2m, 50m, 10m),
            LineItem.Create(invoice.Id, "Item 3", 3m, 25m, 5m)
        );
        await db.SaveChangesAsync();

        var query = InvoiceQuery.Default;

        // Act
        var result = await repository.GetPagedAsync(query);

        // Assert
        result.Items.ShouldContain(i => i.ItemCount == 3);
    }

    [Fact]
    public async Task InvoiceNumber_FormattedCorrectly()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );
        var repository = new InvoiceRepository(testDb.Factory, timeProvider);

        await using var db = await testDb.Factory.CreateDbContextAsync();

        var customer = Customer.Create("Test", "555-0001", "test@test.com", "Test Customer");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var invoice = Invoice.Create(customer.Id, FixedToday, FixedToday.AddDays(30), 10m).Value!;
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var query = InvoiceQuery.Default;

        // Act
        var result = await repository.GetPagedAsync(query);

        // Assert
        result.Items[0].Number.ShouldBe("INV-00001");
    }

    private static async Task SeedInvoicesAsync(TestDatabase testDb)
    {
        await using var db = await testDb.Factory.CreateDbContextAsync();

        // Create customers
        var acmeCorp = Customer.Create("John", "555-0001", "john@acme.com", "Acme Corp");
        var beta = Customer.Create("Jane", "555-0002", "jane@beta.com", "Beta Industries");
        var acmeSolutions = Customer.Create("Bob", "555-0003", "bob@acme.com", "Acme Solutions");
        db.Customers.AddRange(acmeCorp, beta, acmeSolutions);
        await db.SaveChangesAsync();

        var invoices = new[]
        {
            Invoice
                .Create(acmeCorp.Id, FixedToday.AddDays(-10), FixedToday.AddDays(20), 21m)
                .Value!,
            Invoice.Create(beta.Id, FixedToday.AddDays(-5), FixedToday.AddDays(25), 10m).Value!,
            Invoice
                .Create(acmeSolutions.Id, FixedToday.AddDays(-3), FixedToday.AddDays(27), 15m)
                .Value!,
        };

        // Mark second one as Sent
        invoices[1].MarkAsSent(1);

        db.Invoices.AddRange(invoices);
        await db.SaveChangesAsync();

        // Add line items
        foreach (var invoice in invoices)
        {
            db.LineItems.Add(LineItem.Create(invoice.Id, "Service", 1m, 100m, 0m));
        }
        await db.SaveChangesAsync();
    }

    private static (int PrimaryComparison, int IdComparison) CompareByField(
        InvoiceApp.Features.Invoices.Dtos.InvoiceListItemDto a,
        InvoiceApp.Features.Invoices.Dtos.InvoiceListItemDto b,
        InvoiceSortField sortBy
    )
    {
        var primaryComparison = sortBy switch
        {
            InvoiceSortField.Number => a.Id.CompareTo(b.Id),
            InvoiceSortField.CustomerName => string.Compare(
                a.CustomerName,
                b.CustomerName,
                StringComparison.Ordinal
            ),
            InvoiceSortField.IssueDate => a.IssueDate.CompareTo(b.IssueDate),
            InvoiceSortField.DueDate => a.DueDate.CompareTo(b.DueDate),
            InvoiceSortField.ItemCount => a.ItemCount.CompareTo(b.ItemCount),
            InvoiceSortField.Total => a.Total.CompareTo(b.Total),
            _ => 0,
        };

        return (primaryComparison, a.Id.CompareTo(b.Id));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingInvoice_ReturnsInvoice()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );
        var repository = new InvoiceRepository(testDb.Factory, timeProvider);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var customer = Customer.Create("Test", "555-0001", "test@test.com", "Test Corp");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var invoice = Invoice.Create(customer.Id, FixedToday, FixedToday.AddDays(30), 10m).Value!;
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(invoice.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(invoice.Id);
        result.Status.ShouldBe(InvoiceStatus.Draft);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentInvoice_ReturnsNull()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );
        var repository = new InvoiceRepository(testDb.Factory, timeProvider);

        // Act
        var result = await repository.GetByIdAsync(999);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_RemovesInvoiceFromDatabase()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );
        var repository = new InvoiceRepository(testDb.Factory, timeProvider);

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            var customer = Customer.Create("Test", "555-0001", "test@test.com", "Test Corp");
            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            var invoice = Invoice
                .Create(customer.Id, FixedToday, FixedToday.AddDays(30), 10m)
                .Value!;
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();
        }

        // Act
        var invoiceToDelete = await repository.GetByIdAsync(1);
        invoiceToDelete.ShouldNotBeNull();
        await repository.DeleteAsync(invoiceToDelete);

        // Assert
        var deletedInvoice = await repository.GetByIdAsync(1);
        deletedInvoice.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithLineItems_CascadeDeletes()
    {
        // Arrange
        await using var testDb = await TestDatabase.CreateAsync();
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );
        var repository = new InvoiceRepository(testDb.Factory, timeProvider);

        int invoiceId;
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            var customer = Customer.Create("Test", "555-0001", "test@test.com", "Test Corp");
            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            var invoice = Invoice
                .Create(customer.Id, FixedToday, FixedToday.AddDays(30), 10m)
                .Value!;
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();
            invoiceId = invoice.Id;

            db.LineItems.AddRange(
                LineItem.Create(invoice.Id, "Item 1", 1m, 100m, 0m),
                LineItem.Create(invoice.Id, "Item 2", 2m, 50m, 10m)
            );
            await db.SaveChangesAsync();
        }

        // Act
        var invoiceToDelete = await repository.GetByIdAsync(invoiceId);
        invoiceToDelete.ShouldNotBeNull();
        await repository.DeleteAsync(invoiceToDelete);

        // Assert
        await using var verifyDb = await testDb.Factory.CreateDbContextAsync();
        var remainingLineItems = verifyDb.LineItems.Where(l => l.InvoiceId == invoiceId).ToList();
        remainingLineItems.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetWithLineItemCountAsync_ExistingInvoice_ReturnsInvoiceAndCount()
    {
        // Arrange
        await using TestDatabase testDb = await TestDatabase.CreateAsync();
        FakeTimeProvider timeProvider = new(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );
        InvoiceRepository repository = new(testDb.Factory, timeProvider);

        await using (AppDbContext db = await testDb.Factory.CreateDbContextAsync())
        {
            Customer customer = Customer.Create("Test", "555-0001", "test@test.com", "Test Corp");
            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            Invoice newInvoice = Invoice
                .Create(customer.Id, FixedToday, FixedToday.AddDays(30), 10m)
                .Value!;
            db.Invoices.Add(newInvoice);
            await db.SaveChangesAsync();

            db.LineItems.AddRange(
                LineItem.Create(newInvoice.Id, "Item 1", 1m, 100m, 0m),
                LineItem.Create(newInvoice.Id, "Item 2", 2m, 50m, 10m),
                LineItem.Create(newInvoice.Id, "Item 3", 3m, 25m, 5m)
            );
            await db.SaveChangesAsync();
        }

        // Act
        (Invoice? invoice, int itemCount) = await repository.GetWithLineItemCountAsync(1);

        // Assert
        invoice.ShouldNotBeNull();
        invoice.Id.ShouldBe(1);
        itemCount.ShouldBe(3);
    }

    [Fact]
    public async Task GetWithLineItemCountAsync_NonExistent_ReturnsNullInvoice()
    {
        // Arrange
        await using TestDatabase testDb = await TestDatabase.CreateAsync();
        FakeTimeProvider timeProvider = new(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );
        InvoiceRepository repository = new(testDb.Factory, timeProvider);

        // Act
        (Invoice? invoice, int itemCount) = await repository.GetWithLineItemCountAsync(999);

        // Assert
        invoice.ShouldBeNull();
        itemCount.ShouldBe(0);
    }

    [Fact]
    public async Task UpdateAsync_SavesNewStatus()
    {
        // Arrange
        await using TestDatabase testDb = await TestDatabase.CreateAsync();
        FakeTimeProvider timeProvider = new(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );
        InvoiceRepository repository = new(testDb.Factory, timeProvider);

        int invoiceId;
        await using (AppDbContext db = await testDb.Factory.CreateDbContextAsync())
        {
            Customer customer = Customer.Create("Test", "555-0001", "test@test.com", "Test Corp");
            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            Invoice newInvoice = Invoice
                .Create(customer.Id, FixedToday, FixedToday.AddDays(30), 10m)
                .Value!;
            db.Invoices.Add(newInvoice);
            await db.SaveChangesAsync();
            invoiceId = newInvoice.Id;

            db.LineItems.Add(LineItem.Create(newInvoice.Id, "Item 1", 1m, 100m, 0m));
            await db.SaveChangesAsync();
        }

        // Act
        (Invoice? invoice, int itemCount) = await repository.GetWithLineItemCountAsync(invoiceId);
        invoice.ShouldNotBeNull();
        invoice.MarkAsSent(itemCount);
        await repository.UpdateAsync(invoice);

        // Assert
        await using AppDbContext verifyDb = await testDb.Factory.CreateDbContextAsync();
        Invoice? updatedInvoice = await verifyDb.Invoices.FindAsync(invoiceId);
        updatedInvoice.ShouldNotBeNull();
        updatedInvoice.Status.ShouldBe(InvoiceStatus.Sent);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsAllowedNextStatuses()
    {
        // Arrange
        await using TestDatabase testDb = await TestDatabase.CreateAsync();
        FakeTimeProvider timeProvider = new(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );
        InvoiceRepository repository = new(testDb.Factory, timeProvider);

        await using (AppDbContext db = await testDb.Factory.CreateDbContextAsync())
        {
            Customer customer = Customer.Create("Test", "555-0001", "test@test.com", "Test Corp");
            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            Invoice draftInvoice = Invoice
                .Create(customer.Id, FixedToday, FixedToday.AddDays(30), 10m)
                .Value!;
            Invoice sentInvoice = Invoice
                .Create(customer.Id, FixedToday, FixedToday.AddDays(30), 10m)
                .Value!;
            sentInvoice.MarkAsSent(1);
            Invoice paidInvoice = Invoice
                .Create(customer.Id, FixedToday, FixedToday.AddDays(30), 10m)
                .Value!;
            paidInvoice.MarkAsSent(1);
            paidInvoice.MarkAsPaid();
            Invoice cancelledInvoice = Invoice
                .Create(customer.Id, FixedToday, FixedToday.AddDays(30), 10m)
                .Value!;
            cancelledInvoice.Cancel();

            db.Invoices.AddRange(draftInvoice, sentInvoice, paidInvoice, cancelledInvoice);
            await db.SaveChangesAsync();

            db.LineItems.AddRange(
                LineItem.Create(draftInvoice.Id, "Item", 1m, 100m, 0m),
                LineItem.Create(sentInvoice.Id, "Item", 1m, 100m, 0m),
                LineItem.Create(paidInvoice.Id, "Item", 1m, 100m, 0m),
                LineItem.Create(cancelledInvoice.Id, "Item", 1m, 100m, 0m)
            );
            await db.SaveChangesAsync();
        }

        InvoiceQuery query = InvoiceQuery.Default with { PageSize = 100 };

        // Act
        PagedResult<InvoiceListItemDto> result = await repository.GetPagedAsync(query);

        // Assert
        InvoiceListItemDto draft = result.Items.Single(i => i.Status == InvoiceStatus.Draft);
        draft.AllowedNextStatuses.ShouldBe([InvoiceStatus.Sent, InvoiceStatus.Cancelled]);

        InvoiceListItemDto sent = result.Items.Single(i => i.Status == InvoiceStatus.Sent);
        sent.AllowedNextStatuses.ShouldBe([InvoiceStatus.Paid, InvoiceStatus.Cancelled]);

        InvoiceListItemDto paid = result.Items.Single(i => i.Status == InvoiceStatus.Paid);
        paid.AllowedNextStatuses.ShouldBeEmpty();

        InvoiceListItemDto cancelled = result.Items.Single(i =>
            i.Status == InvoiceStatus.Cancelled
        );
        cancelled.AllowedNextStatuses.ShouldBeEmpty();
    }
}
