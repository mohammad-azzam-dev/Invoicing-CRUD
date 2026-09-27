using FluentValidation;
using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Services;
using InvoiceApp.Features.Invoices.Validators;
using InvoiceApp.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace InvoiceApp.Tests.Unit;

public sealed class InvoiceCommandServiceTests
{
    private static readonly DateOnly FixedToday = new(2024, 6, 15);
    private static readonly IValidator<InvoiceFormDto> InvoiceValidator =
        new InvoiceFormDtoValidator();
    private static readonly IValidator<LineItemFormDto> LineItemValidator =
        new LineItemFormDtoValidator();

    private static InvoiceFormValidator CreateFormValidator()
    {
        return new InvoiceFormValidator(
            InvoiceValidator,
            LineItemValidator,
            NullLogger<InvoiceFormValidator>.Instance
        );
    }

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

    [Fact]
    public async Task DeleteAsync_NonExistentInvoice_ReturnsNotFoundError()
    {
        // Arrange
        FakeInvoiceRepository fakeRepository = new();
        InvoiceCommandService service = new(
            fakeRepository,
            CreateFormValidator(),
            NullLogger<InvoiceCommandService>.Instance
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
        InvoiceCommandService service = new(
            fakeRepository,
            CreateFormValidator(),
            NullLogger<InvoiceCommandService>.Instance
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
        InvoiceCommandService service = new(
            fakeRepository,
            CreateFormValidator(),
            NullLogger<InvoiceCommandService>.Instance
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
        InvoiceCommandService service = new(
            fakeRepository,
            CreateFormValidator(),
            NullLogger<InvoiceCommandService>.Instance
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
        InvoiceCommandService service = new(
            fakeRepository,
            CreateFormValidator(),
            NullLogger<InvoiceCommandService>.Instance
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
        InvoiceCommandService service = new(
            fakeRepository,
            CreateFormValidator(),
            NullLogger<InvoiceCommandService>.Instance
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
        InvoiceCommandService service = new(
            fakeRepository,
            CreateFormValidator(),
            NullLogger<InvoiceCommandService>.Instance
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
        InvoiceCommandService service = new(
            fakeRepository,
            CreateFormValidator(),
            NullLogger<InvoiceCommandService>.Instance
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
        InvoiceCommandService service = new(
            fakeRepository,
            CreateFormValidator(),
            NullLogger<InvoiceCommandService>.Instance
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
        InvoiceCommandService service = new(
            fakeRepository,
            CreateFormValidator(),
            NullLogger<InvoiceCommandService>.Instance
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
        InvoiceCommandService service = new(
            fakeRepository,
            CreateFormValidator(),
            NullLogger<InvoiceCommandService>.Instance
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
        InvoiceCommandService service = new(
            fakeRepository,
            CreateFormValidator(),
            NullLogger<InvoiceCommandService>.Instance
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
        InvoiceCommandService service = new(
            fakeRepository,
            CreateFormValidator(),
            NullLogger<InvoiceCommandService>.Instance
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
        InvoiceCommandService service = new(
            fakeRepository,
            CreateFormValidator(),
            NullLogger<InvoiceCommandService>.Instance
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
        InvoiceCommandService service = new(
            fakeRepository,
            CreateFormValidator(),
            NullLogger<InvoiceCommandService>.Instance
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
        InvoiceCommandService service = new(
            fakeRepository,
            CreateFormValidator(),
            NullLogger<InvoiceCommandService>.Instance
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
        InvoiceCommandService service = new(
            fakeRepository,
            CreateFormValidator(),
            NullLogger<InvoiceCommandService>.Instance
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
