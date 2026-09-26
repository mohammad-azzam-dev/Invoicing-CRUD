using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Interfaces;
using InvoiceApp.Tests.TestSupport;
using Shouldly;
using InvoiceListPage = InvoiceApp.Components.Pages.Invoices.Index;

namespace InvoiceApp.Tests.Feature;

public sealed class InvoiceListComponentTests : BunitContext
{
    private static readonly DateOnly FixedToday = new(2024, 6, 15);

    public InvoiceListComponentTests()
    {
        // Register Radzen services
        Services.AddRadzenComponents();

        // Configure JSInterop for Radzen components
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void EmptyList_RendersNoInvoicesMessage()
    {
        // Arrange
        var fakeService = new FakeInvoiceService();
        Services.AddSingleton<IInvoiceService>(fakeService);

        // Act
        var cut = Render<InvoiceListPage>();

        // Assert
        cut.Markup.ShouldContain("No invoices found");
    }

    [Fact]
    public void WithInvoices_RendersInvoiceData()
    {
        // Arrange
        var fakeService = new FakeInvoiceService();
        fakeService.SetInvoices(new List<InvoiceListItemDto>
        {
            new(1, "INV-00001", "Acme Corp", FixedToday, FixedToday.AddDays(30),
                InvoiceStatus.Draft, false, 2, 100m),
            new(2, "INV-00002", "Beta Industries", FixedToday, FixedToday.AddDays(30),
                InvoiceStatus.Sent, false, 3, 250m)
        });
        Services.AddSingleton<IInvoiceService>(fakeService);

        // Act
        var cut = Render<InvoiceListPage>();

        // Assert
        cut.Markup.ShouldContain("Acme Corp");
        cut.Markup.ShouldContain("Beta Industries");
        cut.Markup.ShouldContain("INV-00001");
        cut.Markup.ShouldContain("INV-00002");
    }

    [Fact]
    public void PageTitle_ContainsInvoices()
    {
        // Arrange
        var fakeService = new FakeInvoiceService();
        Services.AddSingleton<IInvoiceService>(fakeService);

        // Act
        var cut = Render<InvoiceListPage>();

        // Assert
        cut.Markup.ShouldContain("Invoices");
    }

    [Fact]
    public void NewInvoiceButton_IsDisabled()
    {
        // Arrange
        var fakeService = new FakeInvoiceService();
        Services.AddSingleton<IInvoiceService>(fakeService);

        // Act
        var cut = Render<InvoiceListPage>();

        // Assert
        cut.Markup.ShouldContain("New Invoice");
        cut.Markup.ShouldContain("disabled");
    }

    [Fact]
    public void OverdueInvoice_ShowsOverdueBadge()
    {
        // Arrange
        var fakeService = new FakeInvoiceService();
        fakeService.SetInvoices(new List<InvoiceListItemDto>
        {
            new(1, "INV-00001", "Overdue Customer", FixedToday.AddDays(-30), FixedToday.AddDays(-5),
                InvoiceStatus.Sent, true, 2, 100m)
        });
        Services.AddSingleton<IInvoiceService>(fakeService);

        // Act
        var cut = Render<InvoiceListPage>();

        // Assert
        cut.Markup.ShouldContain("Overdue");
    }

    [Fact]
    public void StatusBadges_RenderedForAllStatuses()
    {
        // Arrange
        var fakeService = new FakeInvoiceService();
        fakeService.SetInvoices(new List<InvoiceListItemDto>
        {
            new(1, "INV-00001", "Customer 1", FixedToday, FixedToday.AddDays(30),
                InvoiceStatus.Draft, false, 1, 100m),
            new(2, "INV-00002", "Customer 2", FixedToday, FixedToday.AddDays(30),
                InvoiceStatus.Sent, false, 1, 200m),
            new(3, "INV-00003", "Customer 3", FixedToday, FixedToday.AddDays(30),
                InvoiceStatus.Paid, false, 1, 300m),
            new(4, "INV-00004", "Customer 4", FixedToday, FixedToday.AddDays(30),
                InvoiceStatus.Cancelled, false, 1, 400m)
        });
        Services.AddSingleton<IInvoiceService>(fakeService);

        // Act
        var cut = Render<InvoiceListPage>();

        // Assert
        cut.Markup.ShouldContain("Draft");
        cut.Markup.ShouldContain("Sent");
        cut.Markup.ShouldContain("Paid");
        cut.Markup.ShouldContain("Cancelled");
    }

    [Fact]
    public void DeleteButton_VisibleForDraftInvoices()
    {
        // Arrange
        var fakeService = new FakeInvoiceService();
        fakeService.SetInvoices(new List<InvoiceListItemDto>
        {
            new(1, "INV-00001", "Draft Customer", FixedToday, FixedToday.AddDays(30),
                InvoiceStatus.Draft, false, 1, 100m)
        });
        Services.AddSingleton<IInvoiceService>(fakeService);

        // Act
        var cut = Render<InvoiceListPage>();

        // Assert
        var deleteButtons = cut.FindAll("button[title='Delete invoice']");
        deleteButtons.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    public void DeleteButton_HiddenForNonDraftInvoices(InvoiceStatus status)
    {
        // Arrange
        var fakeService = new FakeInvoiceService();
        fakeService.SetInvoices(new List<InvoiceListItemDto>
        {
            new(1, "INV-00001", "Customer", FixedToday, FixedToday.AddDays(30),
                status, false, 1, 100m)
        });
        Services.AddSingleton<IInvoiceService>(fakeService);

        // Act
        var cut = Render<InvoiceListPage>();

        // Assert
        var deleteButtons = cut.FindAll("button[title='Delete invoice']");
        deleteButtons.Count.ShouldBe(0);
    }

    [Fact]
    public void DeleteButton_OnlyVisibleForDraftInMixedList()
    {
        // Arrange
        var fakeService = new FakeInvoiceService();
        fakeService.SetInvoices(new List<InvoiceListItemDto>
        {
            new(1, "INV-00001", "Draft Customer", FixedToday, FixedToday.AddDays(30),
                InvoiceStatus.Draft, false, 1, 100m),
            new(2, "INV-00002", "Sent Customer", FixedToday, FixedToday.AddDays(30),
                InvoiceStatus.Sent, false, 1, 200m),
            new(3, "INV-00003", "Paid Customer", FixedToday, FixedToday.AddDays(30),
                InvoiceStatus.Paid, false, 1, 300m),
            new(4, "INV-00004", "Cancelled Customer", FixedToday, FixedToday.AddDays(30),
                InvoiceStatus.Cancelled, false, 1, 400m)
        });
        Services.AddSingleton<IInvoiceService>(fakeService);

        // Act
        var cut = Render<InvoiceListPage>();

        // Assert - only the Draft invoice should have a delete button
        var deleteButtons = cut.FindAll("button[title='Delete invoice']");
        deleteButtons.Count.ShouldBe(1);
    }
}
