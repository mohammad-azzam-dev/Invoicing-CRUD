using FluentValidation;
using InvoiceApp.Data;
using InvoiceApp.Data.Repositories;
using InvoiceApp.Domain;
using InvoiceApp.Features.Customers.Dtos;
using InvoiceApp.Features.Customers.Interfaces;
using InvoiceApp.Features.Customers.Services;
using InvoiceApp.Features.Customers.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiceApp.Tests.TestSupport;

/// <summary>
/// Helper for setting up customer service tests with real database.
/// </summary>
public sealed class CustomerTestHelper : IAsyncDisposable
{
    private readonly TestDatabase _testDb;

    public IDbContextFactory<AppDbContext> DbFactory => _testDb.Factory;
    public ICustomerRepository Repository { get; }
    public ICustomerService CustomerService { get; }

    private CustomerTestHelper(
        TestDatabase testDb,
        ICustomerRepository repository,
        ICustomerService customerService
    )
    {
        _testDb = testDb;
        Repository = repository;
        CustomerService = customerService;
    }

    public static async Task<CustomerTestHelper> CreateAsync()
    {
        TestDatabase testDb = await TestDatabase.CreateAsync();

        CustomerRepository repository = new(testDb.Factory);

        IValidator<CustomerFormDto> formValidator = new CustomerFormDtoValidator(repository);
        IValidator<CustomerDeleteDto> deleteValidator = new CustomerDeleteDtoValidator(repository);

        CustomerService customerService = new(
            repository,
            formValidator,
            deleteValidator,
            NullLogger<CustomerService>.Instance
        );

        return new CustomerTestHelper(testDb, repository, customerService);
    }

    /// <summary>
    /// Seeds a customer and returns its ID.
    /// </summary>
    public async Task<int> SeedCustomerAsync(
        string name = "Test Customer",
        string email = "test@example.com",
        string phone = "555-0001",
        string? companyName = null,
        string? address = null
    )
    {
        await using AppDbContext db = await DbFactory.CreateDbContextAsync();
        Customer customer = Customer.Create(name, phone, email, companyName, address);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer.Id;
    }

    /// <summary>
    /// Seeds a customer with an invoice to test deletion constraint.
    /// </summary>
    public async Task<int> SeedCustomerWithInvoiceAsync(
        string name = "Customer With Invoice",
        string email = "invoiced@example.com",
        string phone = "555-0002"
    )
    {
        await using AppDbContext db = await DbFactory.CreateDbContextAsync();
        Customer customer = Customer.Create(name, phone, email);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        Invoice invoice = Invoice
            .Create(
                customer.Id,
                DateOnly.FromDateTime(DateTime.Today),
                DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                10m
            )
            .Value!;
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        return customer.Id;
    }

    /// <summary>
    /// Reads a customer from a fresh DbContext.
    /// </summary>
    public async Task<Customer?> ReloadCustomerAsync(int customerId)
    {
        await using AppDbContext db = await DbFactory.CreateDbContextAsync();
        return await db.Customers.FindAsync(customerId);
    }

    /// <summary>
    /// Gets the invoice count for a customer.
    /// </summary>
    public async Task<int> GetInvoiceCountAsync(int customerId)
    {
        await using AppDbContext db = await DbFactory.CreateDbContextAsync();
        return await db.Invoices.CountAsync(i => i.CustomerId == customerId);
    }

    /// <summary>
    /// Counts all customers in the database.
    /// </summary>
    public async Task<int> CountCustomersAsync()
    {
        await using AppDbContext db = await DbFactory.CreateDbContextAsync();
        return await db.Customers.CountAsync();
    }

    /// <summary>
    /// Creates a valid CustomerFormDto.
    /// </summary>
    public static CustomerFormDto CreateValidForm(
        int? id = null,
        string name = "John Doe",
        string email = "john@example.com",
        string phone = "555-1234",
        string? companyName = null,
        string? address = null
    )
    {
        return new CustomerFormDto(id, name, email, phone, companyName, address);
    }

    public ValueTask DisposeAsync() => _testDb.DisposeAsync();
}
