using FluentValidation;
using InvoiceApp.Data;
using InvoiceApp.Data.Repositories;
using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Interfaces;
using InvoiceApp.Features.Invoices.Services;
using InvoiceApp.Features.Invoices.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace InvoiceApp.Tests.TestSupport;

/// <summary>
/// Helper for setting up invoice service tests with real database.
/// </summary>
public sealed class InvoiceTestHelper : IAsyncDisposable
{
    public static readonly DateOnly FixedToday = new(2024, 6, 15);

    private readonly TestDatabase _testDb;
    private readonly FakeTimeProvider _timeProvider;

    public IDbContextFactory<AppDbContext> DbFactory => _testDb.Factory;
    public IInvoiceRepository Repository { get; }
    public IInvoiceCommandService CommandService { get; }
    public IInvoiceQueryService QueryService { get; }

    private InvoiceTestHelper(
        TestDatabase testDb,
        FakeTimeProvider timeProvider,
        IInvoiceRepository repository,
        IInvoiceCommandService commandService,
        IInvoiceQueryService queryService
    )
    {
        _testDb = testDb;
        _timeProvider = timeProvider;
        Repository = repository;
        CommandService = commandService;
        QueryService = queryService;
    }

    public static async Task<InvoiceTestHelper> CreateAsync()
    {
        TestDatabase testDb = await TestDatabase.CreateAsync();
        FakeTimeProvider timeProvider = new(
            new DateTimeOffset(FixedToday, TimeOnly.MinValue, TimeSpan.Zero)
        );

        InvoiceRepository repository = new(testDb.Factory, timeProvider);

        IValidator<InvoiceFormDto> invoiceValidator = new InvoiceFormDtoValidator();
        IValidator<LineItemFormDto> lineItemValidator = new LineItemFormDtoValidator();
        InvoiceFormValidator formValidator = new(
            invoiceValidator,
            lineItemValidator,
            NullLogger<InvoiceFormValidator>.Instance
        );

        InvoiceCommandService commandService = new(
            repository,
            formValidator,
            NullLogger<InvoiceCommandService>.Instance
        );

        InvoiceQueryService queryService = new(
            repository,
            NullLogger<InvoiceQueryService>.Instance
        );

        return new InvoiceTestHelper(testDb, timeProvider, repository, commandService, queryService);
    }

    /// <summary>
    /// Seeds a customer and returns its ID.
    /// </summary>
    public async Task<int> SeedCustomerAsync(
        string name = "Test Customer",
        string? companyName = "Test Company"
    )
    {
        await using AppDbContext db = await DbFactory.CreateDbContextAsync();
        Customer customer = Customer.Create(name, "555-0001", $"{name.Replace(" ", "")}@test.com", companyName);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer.Id;
    }

    /// <summary>
    /// Seeds an invoice with optional line items and returns its ID.
    /// </summary>
    public async Task<int> SeedInvoiceAsync(
        int customerId,
        InvoiceStatus status = InvoiceStatus.Draft,
        DateOnly? issueDate = null,
        DateOnly? dueDate = null,
        decimal taxRate = 10m,
        List<(string Description, decimal Quantity, decimal UnitPrice, decimal DiscountPercent)>? lineItems = null
    )
    {
        await using AppDbContext db = await DbFactory.CreateDbContextAsync();

        Invoice invoice = Invoice.Create(
            customerId,
            issueDate ?? FixedToday,
            dueDate ?? FixedToday.AddDays(30),
            taxRate
        ).Value!;

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        if (lineItems is not null)
        {
            foreach ((string desc, decimal qty, decimal price, decimal discount) in lineItems)
            {
                db.LineItems.Add(LineItem.Create(invoice.Id, desc, qty, price, discount));
            }
            await db.SaveChangesAsync();
        }

        // Apply status transitions if needed
        if (status != InvoiceStatus.Draft)
        {
            int itemCount = lineItems?.Count ?? 0;
            if (status == InvoiceStatus.Sent && itemCount > 0)
            {
                invoice.MarkAsSent(itemCount);
            }
            else if (status == InvoiceStatus.Paid && itemCount > 0)
            {
                invoice.MarkAsSent(itemCount);
                invoice.MarkAsPaid();
            }
            else if (status == InvoiceStatus.Cancelled)
            {
                invoice.Cancel();
            }

            db.Entry(invoice).State = EntityState.Modified;
            await db.SaveChangesAsync();
        }

        return invoice.Id;
    }

    /// <summary>
    /// Reads an invoice with line items from a fresh DbContext.
    /// </summary>
    public async Task<Invoice?> ReloadInvoiceAsync(int invoiceId)
    {
        await using AppDbContext db = await DbFactory.CreateDbContextAsync();
        return await db.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
    }

    /// <summary>
    /// Counts all invoices in the database.
    /// </summary>
    public async Task<int> CountInvoicesAsync()
    {
        await using AppDbContext db = await DbFactory.CreateDbContextAsync();
        return await db.Invoices.CountAsync();
    }

    /// <summary>
    /// Counts all line items in the database.
    /// </summary>
    public async Task<int> CountLineItemsAsync()
    {
        await using AppDbContext db = await DbFactory.CreateDbContextAsync();
        return await db.LineItems.CountAsync();
    }

    /// <summary>
    /// Creates a valid InvoiceFormDto.
    /// </summary>
    public static InvoiceFormDto CreateValidForm(
        int customerId,
        DateOnly? issueDate = null,
        DateOnly? dueDate = null,
        decimal taxRate = 10m
    )
    {
        return new InvoiceFormDto(
            customerId,
            issueDate ?? FixedToday,
            dueDate ?? FixedToday.AddDays(30),
            taxRate
        );
    }

    /// <summary>
    /// Creates a valid LineItemFormDto.
    /// </summary>
    public static LineItemFormDto CreateValidLineItem(
        int id = 0,
        string description = "Test Item",
        decimal quantity = 1m,
        decimal unitPrice = 100m,
        decimal discountPercent = 0m
    )
    {
        return new LineItemFormDto(id, description, quantity, unitPrice, discountPercent);
    }

    public ValueTask DisposeAsync() => _testDb.DisposeAsync();
}
