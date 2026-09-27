using System.Reflection;
using FluentValidation;
using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Services;
using InvoiceApp.Features.Invoices.Validators;
using InvoiceApp.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace InvoiceApp.Tests.Unit;

public sealed class InvoiceServiceTests
{
    private static readonly DateOnly FixedToday = new(2024, 6, 15);
    private static readonly IValidator<InvoiceFormDto> InvoiceValidator =
        new InvoiceFormDtoValidator();
    private static readonly IValidator<LineItemFormDto> LineItemValidator =
        new LineItemFormDtoValidator();

    private static Invoice CreateInvoiceWithId(int id, InvoiceStatus status = InvoiceStatus.Draft)
    {
        Invoice invoice = Invoice.Create(1, FixedToday, FixedToday.AddDays(30), 10m).Value!;
        typeof(Invoice).GetProperty(nameof(Invoice.Id))!.SetValue(invoice, id);

        if (status == InvoiceStatus.Sent)
        {
            invoice.MarkAsSent(1);
        }
        else if (status == InvoiceStatus.Paid)
        {
            invoice.MarkAsSent(1);
            invoice.MarkAsPaid();
        }
        else if (status == InvoiceStatus.Cancelled)
        {
            invoice.Cancel();
        }

        return invoice;
    }

    private static InvoiceListItemDto CreateDto(
        int id,
        string number,
        string customerName,
        DateOnly issueDate,
        DateOnly dueDate,
        InvoiceStatus status,
        bool isOverdue,
        int itemCount,
        decimal total
    )
    {
        return new InvoiceListItemDto(
            id,
            number,
            customerName,
            issueDate,
            dueDate,
            status,
            isOverdue,
            itemCount,
            total,
            InvoiceStatusRules.AllowedNext(status)
        );
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsResultFromRepository()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        List<InvoiceListItemDto> expectedInvoices =
        [
            CreateDto(
                1,
                "INV-00001",
                "Customer A",
                FixedToday,
                FixedToday.AddDays(30),
                InvoiceStatus.Draft,
                false,
                2,
                100m
            ),
            CreateDto(
                2,
                "INV-00002",
                "Customer B",
                FixedToday,
                FixedToday.AddDays(30),
                InvoiceStatus.Sent,
                false,
                3,
                250m
            ),
        ];
        fakeRepository.SetInvoices(expectedInvoices);

        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );
        InvoiceQuery query = InvoiceQuery.Default;

        // Act
        PagedResult<InvoiceListItemDto> result = await service.GetPagedAsync(query);

        // Assert
        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(2);
        result.Items[0].CustomerName.ShouldBe("Customer A");
        result.Items[1].CustomerName.ShouldBe("Customer B");
    }

    [Fact]
    public async Task GetPagedAsync_WithStatusFilter_PassesFilterToRepository()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        List<InvoiceListItemDto> allInvoices =
        [
            CreateDto(
                1,
                "INV-00001",
                "Customer A",
                FixedToday,
                FixedToday.AddDays(30),
                InvoiceStatus.Draft,
                false,
                2,
                100m
            ),
            CreateDto(
                2,
                "INV-00002",
                "Customer B",
                FixedToday,
                FixedToday.AddDays(30),
                InvoiceStatus.Sent,
                false,
                3,
                250m
            ),
            CreateDto(
                3,
                "INV-00003",
                "Customer C",
                FixedToday,
                FixedToday.AddDays(30),
                InvoiceStatus.Sent,
                true,
                1,
                150m
            ),
        ];
        fakeRepository.SetInvoices(allInvoices);

        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );
        InvoiceQuery query = InvoiceQuery.Default with { Status = InvoiceStatus.Sent };

        // Act
        PagedResult<InvoiceListItemDto> result = await service.GetPagedAsync(query);

        // Assert
        result.Items.Count.ShouldBe(2);
        result.Items.ShouldAllBe(i => i.Status == InvoiceStatus.Sent);
    }

    [Fact]
    public async Task GetPagedAsync_WithSearchFilter_PassesSearchToRepository()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        List<InvoiceListItemDto> allInvoices =
        [
            CreateDto(
                1,
                "INV-00001",
                "Acme Corp",
                FixedToday,
                FixedToday.AddDays(30),
                InvoiceStatus.Draft,
                false,
                2,
                100m
            ),
            CreateDto(
                2,
                "INV-00002",
                "Beta Industries",
                FixedToday,
                FixedToday.AddDays(30),
                InvoiceStatus.Sent,
                false,
                3,
                250m
            ),
            CreateDto(
                3,
                "INV-00003",
                "Acme Solutions",
                FixedToday,
                FixedToday.AddDays(30),
                InvoiceStatus.Sent,
                true,
                1,
                150m
            ),
        ];
        fakeRepository.SetInvoices(allInvoices);

        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );
        InvoiceQuery query = InvoiceQuery.Default with { Search = "Acme" };

        // Act
        PagedResult<InvoiceListItemDto> result = await service.GetPagedAsync(query);

        // Assert
        result.Items.Count.ShouldBe(2);
        result.Items.ShouldAllBe(i => i.CustomerName.Contains("Acme"));
    }

    [Fact]
    public async Task GetPagedAsync_WithPaging_ReturnsCorrectPage()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        List<InvoiceListItemDto> allInvoices = Enumerable
            .Range(1, 25)
            .Select(i =>
                CreateDto(
                    i,
                    $"INV-{i:D5}",
                    $"Customer {i}",
                    FixedToday,
                    FixedToday.AddDays(30),
                    InvoiceStatus.Draft,
                    false,
                    1,
                    i * 100m
                )
            )
            .ToList();
        fakeRepository.SetInvoices(allInvoices);

        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );
        InvoiceQuery query = InvoiceQuery.Default with
        {
            Page = 2,
            PageSize = 10,
            SortBy = InvoiceSortField.Number,
            Descending = false,
        };

        // Act
        PagedResult<InvoiceListItemDto> result = await service.GetPagedAsync(query);

        // Assert
        result.TotalCount.ShouldBe(25);
        result.Items.Count.ShouldBe(10);
        result.Items[0].Id.ShouldBe(11); // Second page starts at 11
    }

    [Fact]
    public async Task GetPagedAsync_EmptyRepository_ReturnsEmptyResult()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );
        InvoiceQuery query = InvoiceQuery.Default;

        // Act
        PagedResult<InvoiceListItemDto> result = await service.GetPagedAsync(query);

        // Assert
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentInvoice_ReturnsNotFoundError()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );

        // Act
        Result result = await service.DeleteAsync(999);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Invoice not found.");
    }

    [Theory]
    [InlineData(InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    public async Task DeleteAsync_NonDraftInvoice_ReturnsBusinessRuleError(InvoiceStatus status)
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        Invoice invoice = CreateInvoiceWithId(1, status);
        fakeRepository.SetInvoiceEntity(invoice);
        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );

        // Act
        Result result = await service.DeleteAsync(1);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Only draft invoices can be deleted.");
        fakeRepository.WasDeleted(1).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAsync_DraftInvoice_DeletesSuccessfully()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        Invoice invoice = CreateInvoiceWithId(1, InvoiceStatus.Draft);
        fakeRepository.SetInvoiceEntity(invoice);
        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );

        // Act
        Result result = await service.DeleteAsync(1);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        fakeRepository.WasDeleted(1).ShouldBeTrue();
    }

    [Fact]
    public async Task ChangeStatusAsync_NonExistentInvoice_ReturnsNotFoundError()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );

        // Act
        Result result = await service.ChangeStatusAsync(999, InvoiceStatus.Sent);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Invoice not found.");
    }

    [Fact]
    public async Task ChangeStatusAsync_DraftToSent_WithItems_Succeeds()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        Invoice invoice = CreateInvoiceWithId(1, InvoiceStatus.Draft);
        fakeRepository.SetInvoiceEntity(invoice, lineItemCount: 2);
        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );

        // Act
        Result result = await service.ChangeStatusAsync(1, InvoiceStatus.Sent);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        fakeRepository.WasUpdated(1).ShouldBeTrue();
    }

    [Fact]
    public async Task ChangeStatusAsync_DraftToSent_WithZeroItems_ReturnsFailure()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        Invoice invoice = CreateInvoiceWithId(1, InvoiceStatus.Draft);
        fakeRepository.SetInvoiceEntity(invoice, lineItemCount: 0);
        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );

        // Act
        Result result = await service.ChangeStatusAsync(1, InvoiceStatus.Sent);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Cannot send an invoice without line items.");
        fakeRepository.WasUpdated(1).ShouldBeFalse();
    }

    [Fact]
    public async Task ChangeStatusAsync_DraftToCancelled_Succeeds()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        Invoice invoice = CreateInvoiceWithId(1, InvoiceStatus.Draft);
        fakeRepository.SetInvoiceEntity(invoice);
        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );

        // Act
        Result result = await service.ChangeStatusAsync(1, InvoiceStatus.Cancelled);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        fakeRepository.WasUpdated(1).ShouldBeTrue();
    }

    [Fact]
    public async Task ChangeStatusAsync_SentToPaid_Succeeds()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        Invoice invoice = CreateInvoiceWithId(1, InvoiceStatus.Sent);
        fakeRepository.SetInvoiceEntity(invoice);
        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );

        // Act
        Result result = await service.ChangeStatusAsync(1, InvoiceStatus.Paid);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        fakeRepository.WasUpdated(1).ShouldBeTrue();
    }

    [Fact]
    public async Task ChangeStatusAsync_SentToCancelled_Succeeds()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        Invoice invoice = CreateInvoiceWithId(1, InvoiceStatus.Sent);
        fakeRepository.SetInvoiceEntity(invoice);
        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );

        // Act
        Result result = await service.ChangeStatusAsync(1, InvoiceStatus.Cancelled);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        fakeRepository.WasUpdated(1).ShouldBeTrue();
    }

    [Fact]
    public async Task ChangeStatusAsync_DraftToPaid_ReturnsFailure()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        Invoice invoice = CreateInvoiceWithId(1, InvoiceStatus.Draft);
        fakeRepository.SetInvoiceEntity(invoice);
        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );

        // Act
        Result result = await service.ChangeStatusAsync(1, InvoiceStatus.Paid);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Cannot transition from Draft to Paid");
        fakeRepository.WasUpdated(1).ShouldBeFalse();
    }

    [Fact]
    public async Task ChangeStatusAsync_SentToDraft_ReturnsFailure()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        Invoice invoice = CreateInvoiceWithId(1, InvoiceStatus.Sent);
        fakeRepository.SetInvoiceEntity(invoice);
        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );

        // Act
        Result result = await service.ChangeStatusAsync(1, InvoiceStatus.Draft);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Cannot transition to Draft");
        fakeRepository.WasUpdated(1).ShouldBeFalse();
    }

    [Theory]
    [InlineData(InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Cancelled)]
    public async Task ChangeStatusAsync_PaidToAny_ReturnsFailure(InvoiceStatus newStatus)
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        Invoice invoice = CreateInvoiceWithId(1, InvoiceStatus.Paid);
        fakeRepository.SetInvoiceEntity(invoice, lineItemCount: 1);
        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );

        // Act
        Result result = await service.ChangeStatusAsync(1, newStatus);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Cannot transition from Paid");
        fakeRepository.WasUpdated(1).ShouldBeFalse();
    }

    [Fact]
    public async Task ChangeStatusAsync_PaidToDraft_ReturnsFailure()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        Invoice invoice = CreateInvoiceWithId(1, InvoiceStatus.Paid);
        fakeRepository.SetInvoiceEntity(invoice);
        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );

        // Act
        Result result = await service.ChangeStatusAsync(1, InvoiceStatus.Draft);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Cannot transition to Draft");
        fakeRepository.WasUpdated(1).ShouldBeFalse();
    }

    [Theory]
    [InlineData(InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Paid)]
    public async Task ChangeStatusAsync_CancelledToAny_ReturnsFailure(InvoiceStatus newStatus)
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        Invoice invoice = CreateInvoiceWithId(1, InvoiceStatus.Cancelled);
        fakeRepository.SetInvoiceEntity(invoice, lineItemCount: 1);
        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );

        // Act
        Result result = await service.ChangeStatusAsync(1, newStatus);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Cannot transition from Cancelled");
        fakeRepository.WasUpdated(1).ShouldBeFalse();
    }

    [Fact]
    public async Task ChangeStatusAsync_CancelledToDraft_ReturnsFailure()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        Invoice invoice = CreateInvoiceWithId(1, InvoiceStatus.Cancelled);
        fakeRepository.SetInvoiceEntity(invoice);
        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );

        // Act
        Result result = await service.ChangeStatusAsync(1, InvoiceStatus.Draft);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Cannot transition to Draft");
        fakeRepository.WasUpdated(1).ShouldBeFalse();
    }

    // Tests for SaveAsync validation (DbContext-independent)

    [Fact]
    public async Task SaveAsync_CreateWithInvalidForm_ReturnsFailure()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );

        InvoiceFormDto form = new(0, FixedToday, FixedToday.AddDays(30), 10m); // Invalid: CustomerId is 0
        List<LineItemFormDto> lineItems = [new(0, "Widget", 2m, 10m, 0m)];

        // Act
        Result<int> result = await service.SaveAsync(0, form, lineItems);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
    }

    [Fact]
    public async Task SaveAsync_CreateWithInvalidLineItem_ReturnsFailure()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        InvoiceService service = new(
            fakeRepository,
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceService>.Instance
        );

        InvoiceFormDto form = new(1, FixedToday, FixedToday.AddDays(30), 10m);
        List<LineItemFormDto> lineItems = [new(0, "", 2m, 10m, 0m)]; // Invalid: empty description

        // Act
        Result<int> result = await service.SaveAsync(0, form, lineItems);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Description is required");
    }

    // Note: SaveAsync tests that involve actual database operations (create, update, delete)
    // are covered in Feature tests since SaveAsync now uses DbContext directly for transactions.
}
