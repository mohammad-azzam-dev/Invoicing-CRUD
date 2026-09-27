using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Services;
using InvoiceApp.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace InvoiceApp.Tests.Unit;

public sealed class InvoiceQueryServiceTests
{
    private static readonly DateOnly FixedToday = new(2024, 6, 15);

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

        InvoiceQueryService service = new(
            fakeRepository,
            NullLogger<InvoiceQueryService>.Instance
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

        InvoiceQueryService service = new(
            fakeRepository,
            NullLogger<InvoiceQueryService>.Instance
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

        InvoiceQueryService service = new(
            fakeRepository,
            NullLogger<InvoiceQueryService>.Instance
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

        InvoiceQueryService service = new(
            fakeRepository,
            NullLogger<InvoiceQueryService>.Instance
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
        InvoiceQueryService service = new(
            fakeRepository,
            NullLogger<InvoiceQueryService>.Instance
        );
        InvoiceQuery query = InvoiceQuery.Default;

        // Act
        PagedResult<InvoiceListItemDto> result = await service.GetPagedAsync(query);

        // Assert
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }
}
