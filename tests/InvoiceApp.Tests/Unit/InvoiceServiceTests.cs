using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Services;
using InvoiceApp.Tests.TestSupport;
using Shouldly;

namespace InvoiceApp.Tests.Unit;

public sealed class InvoiceServiceTests
{
    private static readonly DateOnly FixedToday = new(2024, 6, 15);

    private static Invoice CreateInvoiceWithId(int id, InvoiceStatus status = InvoiceStatus.Draft)
    {
        var invoice = Invoice.Create(1, FixedToday, FixedToday.AddDays(30), 10m).Value!;
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
    public async Task GetPagedAsync_ReturnsResultFromRepository()
    {
        // Arrange
        var fakeRepository = new FakeInvoiceRepository();
        var expectedInvoices = new List<InvoiceListItemDto>
        {
            new(1, "INV-00001", "Customer A", FixedToday, FixedToday.AddDays(30), InvoiceStatus.Draft, false, 2, 100m),
            new(2, "INV-00002", "Customer B", FixedToday, FixedToday.AddDays(30), InvoiceStatus.Sent, false, 3, 250m)
        };
        fakeRepository.SetInvoices(expectedInvoices);

        var service = new InvoiceService(fakeRepository, NullLogger<InvoiceService>.Instance);
        var query = InvoiceQuery.Default;

        // Act
        var result = await service.GetPagedAsync(query);

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
        var fakeRepository = new FakeInvoiceRepository();
        var allInvoices = new List<InvoiceListItemDto>
        {
            new(1, "INV-00001", "Customer A", FixedToday, FixedToday.AddDays(30), InvoiceStatus.Draft, false, 2, 100m),
            new(2, "INV-00002", "Customer B", FixedToday, FixedToday.AddDays(30), InvoiceStatus.Sent, false, 3, 250m),
            new(3, "INV-00003", "Customer C", FixedToday, FixedToday.AddDays(30), InvoiceStatus.Sent, true, 1, 150m)
        };
        fakeRepository.SetInvoices(allInvoices);

        var service = new InvoiceService(fakeRepository, NullLogger<InvoiceService>.Instance);
        var query = InvoiceQuery.Default with { Status = InvoiceStatus.Sent };

        // Act
        var result = await service.GetPagedAsync(query);

        // Assert
        result.Items.Count.ShouldBe(2);
        result.Items.ShouldAllBe(i => i.Status == InvoiceStatus.Sent);
    }

    [Fact]
    public async Task GetPagedAsync_WithSearchFilter_PassesSearchToRepository()
    {
        // Arrange
        var fakeRepository = new FakeInvoiceRepository();
        var allInvoices = new List<InvoiceListItemDto>
        {
            new(1, "INV-00001", "Acme Corp", FixedToday, FixedToday.AddDays(30), InvoiceStatus.Draft, false, 2, 100m),
            new(2, "INV-00002", "Beta Industries", FixedToday, FixedToday.AddDays(30), InvoiceStatus.Sent, false, 3, 250m),
            new(3, "INV-00003", "Acme Solutions", FixedToday, FixedToday.AddDays(30), InvoiceStatus.Sent, true, 1, 150m)
        };
        fakeRepository.SetInvoices(allInvoices);

        var service = new InvoiceService(fakeRepository, NullLogger<InvoiceService>.Instance);
        var query = InvoiceQuery.Default with { Search = "Acme" };

        // Act
        var result = await service.GetPagedAsync(query);

        // Assert
        result.Items.Count.ShouldBe(2);
        result.Items.ShouldAllBe(i => i.CustomerName.Contains("Acme"));
    }

    [Fact]
    public async Task GetPagedAsync_WithPaging_ReturnsCorrectPage()
    {
        // Arrange
        var fakeRepository = new FakeInvoiceRepository();
        var allInvoices = Enumerable.Range(1, 25)
            .Select(i => new InvoiceListItemDto(
                i, $"INV-{i:D5}", $"Customer {i}", FixedToday, FixedToday.AddDays(30),
                InvoiceStatus.Draft, false, 1, i * 100m))
            .ToList();
        fakeRepository.SetInvoices(allInvoices);

        var service = new InvoiceService(fakeRepository, NullLogger<InvoiceService>.Instance);
        var query = InvoiceQuery.Default with { Page = 2, PageSize = 10, SortBy = InvoiceSortField.Number, Descending = false };

        // Act
        var result = await service.GetPagedAsync(query);

        // Assert
        result.TotalCount.ShouldBe(25);
        result.Items.Count.ShouldBe(10);
        result.Items[0].Id.ShouldBe(11); // Second page starts at 11
    }

    [Fact]
    public async Task GetPagedAsync_EmptyRepository_ReturnsEmptyResult()
    {
        // Arrange
        var fakeRepository = new FakeInvoiceRepository();
        var service = new InvoiceService(fakeRepository, NullLogger<InvoiceService>.Instance);
        var query = InvoiceQuery.Default;

        // Act
        var result = await service.GetPagedAsync(query);

        // Assert
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentInvoice_ReturnsNotFoundError()
    {
        // Arrange
        var fakeRepository = new FakeInvoiceRepository();
        var service = new InvoiceService(fakeRepository, NullLogger<InvoiceService>.Instance);

        // Act
        var result = await service.DeleteAsync(999);

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
        var fakeRepository = new FakeInvoiceRepository();
        var invoice = CreateInvoiceWithId(1, status);
        fakeRepository.SetInvoiceEntity(invoice);
        var service = new InvoiceService(fakeRepository, NullLogger<InvoiceService>.Instance);

        // Act
        var result = await service.DeleteAsync(1);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Only draft invoices can be deleted.");
        fakeRepository.WasDeleted(1).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAsync_DraftInvoice_DeletesSuccessfully()
    {
        // Arrange
        var fakeRepository = new FakeInvoiceRepository();
        var invoice = CreateInvoiceWithId(1, InvoiceStatus.Draft);
        fakeRepository.SetInvoiceEntity(invoice);
        var service = new InvoiceService(fakeRepository, NullLogger<InvoiceService>.Instance);

        // Act
        var result = await service.DeleteAsync(1);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        fakeRepository.WasDeleted(1).ShouldBeTrue();
    }
}
