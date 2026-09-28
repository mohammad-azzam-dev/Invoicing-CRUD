using InvoiceApp.Data;
using InvoiceApp.Data.Repositories;
using InvoiceApp.Domain;
using InvoiceApp.Features.Customers;
using InvoiceApp.Features.Customers.Dtos;
using InvoiceApp.Features.Invoices;
using InvoiceApp.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace InvoiceApp.Tests.Feature;

public sealed class CustomerRepositoryTests
{
    [Fact]
    public async Task Search_ByName_ReturnsMatchingCustomers()
    {
        // Arrange
        await using TestDatabase testDb = await TestDatabase.CreateAsync();
        CustomerRepository repository = new(testDb.Factory);

        await SeedCustomersAsync(testDb);

        CustomerQuery query = CustomerQuery.Default with { Search = "John" };

        // Act
        PagedResult<CustomerListItemDto> result = await repository.GetPagedAsync(query);

        // Assert
        result.Items.ShouldNotBeEmpty();
        result.Items.ShouldAllBe(c =>
            c.Name.Contains("John", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public async Task Search_ByCompanyName_ReturnsMatchingCustomers()
    {
        // Arrange
        await using TestDatabase testDb = await TestDatabase.CreateAsync();
        CustomerRepository repository = new(testDb.Factory);

        await SeedCustomersAsync(testDb);

        CustomerQuery query = CustomerQuery.Default with { Search = "Acme" };

        // Act
        PagedResult<CustomerListItemDto> result = await repository.GetPagedAsync(query);

        // Assert
        result.Items.ShouldNotBeEmpty();
        result.Items.ShouldAllBe(c =>
            c.CompanyName != null && c.CompanyName.Contains("Acme", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public async Task Search_ByEmail_ReturnsMatchingCustomers()
    {
        // Arrange
        await using TestDatabase testDb = await TestDatabase.CreateAsync();
        CustomerRepository repository = new(testDb.Factory);

        await SeedCustomersAsync(testDb);

        CustomerQuery query = CustomerQuery.Default with { Search = "john@" };

        // Act
        PagedResult<CustomerListItemDto> result = await repository.GetPagedAsync(query);

        // Assert
        result.Items.ShouldNotBeEmpty();
        result.Items.ShouldAllBe(c =>
            c.Email.Contains("john@", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public async Task Search_ByPhone_ReturnsMatchingCustomers()
    {
        // Arrange
        await using TestDatabase testDb = await TestDatabase.CreateAsync();
        CustomerRepository repository = new(testDb.Factory);

        await SeedCustomersAsync(testDb);

        CustomerQuery query = CustomerQuery.Default with { Search = "555-0001" };

        // Act
        PagedResult<CustomerListItemDto> result = await repository.GetPagedAsync(query);

        // Assert
        result.Items.ShouldNotBeEmpty();
        result.Items.ShouldAllBe(c =>
            c.Phone.Contains("555-0001", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Theory]
    [InlineData(CustomerSortField.DisplayName, false)]
    [InlineData(CustomerSortField.DisplayName, true)]
    [InlineData(CustomerSortField.Name, false)]
    [InlineData(CustomerSortField.Name, true)]
    [InlineData(CustomerSortField.Email, false)]
    [InlineData(CustomerSortField.Email, true)]
    [InlineData(CustomerSortField.Phone, false)]
    [InlineData(CustomerSortField.Phone, true)]
    [InlineData(CustomerSortField.InvoiceCount, false)]
    [InlineData(CustomerSortField.InvoiceCount, true)]
    public async Task Sort_AllFields_ReturnsSortedResults(CustomerSortField sortBy, bool descending)
    {
        // Arrange
        await using TestDatabase testDb = await TestDatabase.CreateAsync();
        CustomerRepository repository = new(testDb.Factory);

        await SeedCustomersAsync(testDb);

        CustomerQuery query = CustomerQuery.Default with
        {
            SortBy = sortBy,
            Descending = descending,
            PageSize = 100,
        };

        // Act
        PagedResult<CustomerListItemDto> result = await repository.GetPagedAsync(query);

        // Assert
        result.Items.Count.ShouldBeGreaterThan(1);

        List<CustomerListItemDto> items = result.Items.ToList();
        for (int i = 1; i < items.Count; i++)
        {
            (int primary, int id) = CompareByField(items[i - 1], items[i], sortBy);

            if (descending)
            {
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
    public async Task Paging_ReturnsCorrectPage()
    {
        // Arrange
        await using TestDatabase testDb = await TestDatabase.CreateAsync();
        CustomerRepository repository = new(testDb.Factory);

        await SeedCustomersAsync(testDb);

        CustomerQuery page1Query = CustomerQuery.Default with { Page = 1, PageSize = 2 };
        CustomerQuery page2Query = CustomerQuery.Default with { Page = 2, PageSize = 2 };

        // Act
        PagedResult<CustomerListItemDto> page1 = await repository.GetPagedAsync(page1Query);
        PagedResult<CustomerListItemDto> page2 = await repository.GetPagedAsync(page2Query);

        // Assert
        page1.Items.Count.ShouldBe(2);
        page2.Items.Count.ShouldBeGreaterThanOrEqualTo(1);
        page1.TotalCount.ShouldBe(page2.TotalCount);

        // Pages should have different items
        page1.Items.Select(c => c.Id).ShouldNotBe(page2.Items.Select(c => c.Id));
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectInvoiceCount()
    {
        // Arrange
        await using TestDatabase testDb = await TestDatabase.CreateAsync();
        CustomerRepository repository = new(testDb.Factory);

        await using AppDbContext db = await testDb.Factory.CreateDbContextAsync();
        Customer customer = Customer.Create("Test Customer", "555-0001", "test@example.com");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        // Add 2 invoices
        for (int i = 0; i < 2; i++)
        {
            Invoice invoice = Invoice.Create(
                customer.Id,
                DateOnly.FromDateTime(DateTime.Today),
                DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                10m
            ).Value!;
            db.Invoices.Add(invoice);
        }
        await db.SaveChangesAsync();

        CustomerQuery query = CustomerQuery.Default;

        // Act
        PagedResult<CustomerListItemDto> result = await repository.GetPagedAsync(query);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].InvoiceCount.ShouldBe(2);
    }

    [Fact]
    public async Task EmailExistsAsync_ExistingEmail_ReturnsTrue()
    {
        // Arrange
        await using TestDatabase testDb = await TestDatabase.CreateAsync();
        CustomerRepository repository = new(testDb.Factory);

        await using AppDbContext db = await testDb.Factory.CreateDbContextAsync();
        Customer customer = Customer.Create("Test", "555-0001", "existing@example.com");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        // Act
        bool exists = await repository.EmailExistsAsync("existing@example.com");

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_ExistingEmailDifferentCase_ReturnsTrue()
    {
        // Arrange
        await using TestDatabase testDb = await TestDatabase.CreateAsync();
        CustomerRepository repository = new(testDb.Factory);

        await using AppDbContext db = await testDb.Factory.CreateDbContextAsync();
        Customer customer = Customer.Create("Test", "555-0001", "existing@example.com");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        // Act
        bool exists = await repository.EmailExistsAsync("EXISTING@EXAMPLE.COM");

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_WithExcludeId_ExcludesSelf()
    {
        // Arrange
        await using TestDatabase testDb = await TestDatabase.CreateAsync();
        CustomerRepository repository = new(testDb.Factory);

        await using AppDbContext db = await testDb.Factory.CreateDbContextAsync();
        Customer customer = Customer.Create("Test", "555-0001", "test@example.com");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        // Act
        bool exists = await repository.EmailExistsAsync("test@example.com", customer.Id);

        // Assert
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task DisplayName_UsesCompanyNameWhenPresent()
    {
        // Arrange
        await using TestDatabase testDb = await TestDatabase.CreateAsync();
        CustomerRepository repository = new(testDb.Factory);

        await using AppDbContext db = await testDb.Factory.CreateDbContextAsync();
        Customer customer = Customer.Create("John Doe", "555-0001", "john@example.com", "Acme Corp");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        CustomerQuery query = CustomerQuery.Default;

        // Act
        PagedResult<CustomerListItemDto> result = await repository.GetPagedAsync(query);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].DisplayName.ShouldBe("Acme Corp");
        result.Items[0].Name.ShouldBe("John Doe");
    }

    [Fact]
    public async Task DisplayName_UsesNameWhenNoCompanyName()
    {
        // Arrange
        await using TestDatabase testDb = await TestDatabase.CreateAsync();
        CustomerRepository repository = new(testDb.Factory);

        await using AppDbContext db = await testDb.Factory.CreateDbContextAsync();
        Customer customer = Customer.Create("John Doe", "555-0001", "john@example.com");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        CustomerQuery query = CustomerQuery.Default;

        // Act
        PagedResult<CustomerListItemDto> result = await repository.GetPagedAsync(query);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].DisplayName.ShouldBe("John Doe");
    }

    private static (int Primary, int Id) CompareByField(
        CustomerListItemDto a,
        CustomerListItemDto b,
        CustomerSortField sortBy
    )
    {
        int primary = sortBy switch
        {
            CustomerSortField.DisplayName => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase),
            CustomerSortField.Name => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase),
            CustomerSortField.Email => string.Compare(a.Email, b.Email, StringComparison.OrdinalIgnoreCase),
            CustomerSortField.Phone => string.Compare(a.Phone, b.Phone, StringComparison.OrdinalIgnoreCase),
            CustomerSortField.InvoiceCount => a.InvoiceCount.CompareTo(b.InvoiceCount),
            _ => 0,
        };

        int id = a.Id.CompareTo(b.Id);
        return (primary, id);
    }

    private static async Task SeedCustomersAsync(TestDatabase testDb)
    {
        await using AppDbContext db = await testDb.Factory.CreateDbContextAsync();

        List<Customer> customers =
        [
            Customer.Create("John Doe", "555-0001", "john@example.com", "Acme Corp"),
            Customer.Create("Jane Smith", "555-0002", "jane@example.com", "Tech Inc"),
            Customer.Create("Bob Wilson", "555-0003", "bob@example.com"),
            Customer.Create("Alice Brown", "555-0004", "alice@example.com", "ABC Company"),
        ];

        db.Customers.AddRange(customers);
        await db.SaveChangesAsync();

        // Add some invoices to vary invoice count
        Invoice invoice = Invoice.Create(
            customers[0].Id,
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            10m
        ).Value!;
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
    }
}
