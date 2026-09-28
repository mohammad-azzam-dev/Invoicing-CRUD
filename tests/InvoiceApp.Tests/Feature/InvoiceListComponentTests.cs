using Bunit;
using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Interfaces;
using InvoiceApp.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
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
    public void EmptyList_RendersNoInvoicesMessage()
    {
        // Arrange
        FakeInvoiceQueryService fakeQueryService = new();
        FakeInvoiceCommandService fakeCommandService = new();
        Services.AddSingleton<IInvoiceQueryService>(fakeQueryService);
        Services.AddSingleton<IInvoiceCommandService>(fakeCommandService);

        // Act
        IRenderedComponent<InvoiceListPage> cut = Render<InvoiceListPage>();

        // Assert
        cut.Markup.ShouldContain("No invoices found");
    }

    [Fact]
    public void WithInvoices_RendersInvoiceData()
    {
        // Arrange
        FakeInvoiceQueryService fakeQueryService = new();
        FakeInvoiceCommandService fakeCommandService = new();
        fakeQueryService.SetInvoices(
            new List<InvoiceListItemDto>
            {
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
            }
        );
        Services.AddSingleton<IInvoiceQueryService>(fakeQueryService);
        Services.AddSingleton<IInvoiceCommandService>(fakeCommandService);

        // Act
        IRenderedComponent<InvoiceListPage> cut = Render<InvoiceListPage>();

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
        FakeInvoiceQueryService fakeQueryService = new();
        FakeInvoiceCommandService fakeCommandService = new();
        Services.AddSingleton<IInvoiceQueryService>(fakeQueryService);
        Services.AddSingleton<IInvoiceCommandService>(fakeCommandService);

        // Act
        IRenderedComponent<InvoiceListPage> cut = Render<InvoiceListPage>();

        // Assert
        cut.Markup.ShouldContain("Invoices");
    }

    [Fact]
    public void OverdueInvoice_ShowsOverdueBadge()
    {
        // Arrange
        FakeInvoiceQueryService fakeQueryService = new();
        FakeInvoiceCommandService fakeCommandService = new();
        fakeQueryService.SetInvoices(
            new List<InvoiceListItemDto>
            {
                CreateDto(
                    1,
                    "INV-00001",
                    "Overdue Customer",
                    FixedToday.AddDays(-30),
                    FixedToday.AddDays(-5),
                    InvoiceStatus.Sent,
                    true,
                    2,
                    100m
                ),
            }
        );
        Services.AddSingleton<IInvoiceQueryService>(fakeQueryService);
        Services.AddSingleton<IInvoiceCommandService>(fakeCommandService);

        // Act
        IRenderedComponent<InvoiceListPage> cut = Render<InvoiceListPage>();

        // Assert
        cut.Markup.ShouldContain("Overdue");
    }

    [Fact]
    public void StatusBadges_RenderedForAllStatuses()
    {
        // Arrange
        FakeInvoiceQueryService fakeQueryService = new();
        FakeInvoiceCommandService fakeCommandService = new();
        fakeQueryService.SetInvoices(
            new List<InvoiceListItemDto>
            {
                CreateDto(
                    1,
                    "INV-00001",
                    "Customer 1",
                    FixedToday,
                    FixedToday.AddDays(30),
                    InvoiceStatus.Draft,
                    false,
                    1,
                    100m
                ),
                CreateDto(
                    2,
                    "INV-00002",
                    "Customer 2",
                    FixedToday,
                    FixedToday.AddDays(30),
                    InvoiceStatus.Sent,
                    false,
                    1,
                    200m
                ),
                CreateDto(
                    3,
                    "INV-00003",
                    "Customer 3",
                    FixedToday,
                    FixedToday.AddDays(30),
                    InvoiceStatus.Paid,
                    false,
                    1,
                    300m
                ),
                CreateDto(
                    4,
                    "INV-00004",
                    "Customer 4",
                    FixedToday,
                    FixedToday.AddDays(30),
                    InvoiceStatus.Cancelled,
                    false,
                    1,
                    400m
                ),
            }
        );
        Services.AddSingleton<IInvoiceQueryService>(fakeQueryService);
        Services.AddSingleton<IInvoiceCommandService>(fakeCommandService);

        // Act
        IRenderedComponent<InvoiceListPage> cut = Render<InvoiceListPage>();

        // Assert
        cut.Markup.ShouldContain("Draft");
        cut.Markup.ShouldContain("Sent");
        cut.Markup.ShouldContain("Paid");
        cut.Markup.ShouldContain("Cancelled");
    }

    [Fact]
    public void ActionsMenu_VisibleForDraftInvoices()
    {
        // Arrange
        FakeInvoiceQueryService fakeQueryService = new();
        FakeInvoiceCommandService fakeCommandService = new();
        fakeQueryService.SetInvoices(
            new List<InvoiceListItemDto>
            {
                CreateDto(
                    1,
                    "INV-00001",
                    "Draft Customer",
                    FixedToday,
                    FixedToday.AddDays(30),
                    InvoiceStatus.Draft,
                    false,
                    1,
                    100m
                ),
            }
        );
        Services.AddSingleton<IInvoiceQueryService>(fakeQueryService);
        Services.AddSingleton<IInvoiceCommandService>(fakeCommandService);

        // Act
        IRenderedComponent<InvoiceListPage> cut = Render<InvoiceListPage>();

        // Assert - Draft invoices have a three-dots menu with more_vert icon
        cut.Markup.ShouldContain("more_vert");
    }

    [Theory]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    public void ActionsMenu_VisibleForFinalStatusInvoices_WithViewOnly(InvoiceStatus status)
    {
        // Arrange
        FakeInvoiceQueryService fakeQueryService = new();
        FakeInvoiceCommandService fakeCommandService = new();
        fakeQueryService.SetInvoices(
            new List<InvoiceListItemDto>
            {
                CreateDto(
                    1,
                    "INV-00001",
                    "Customer",
                    FixedToday,
                    FixedToday.AddDays(30),
                    status,
                    false,
                    1,
                    100m
                ),
            }
        );
        Services.AddSingleton<IInvoiceQueryService>(fakeQueryService);
        Services.AddSingleton<IInvoiceCommandService>(fakeCommandService);

        // Act
        IRenderedComponent<InvoiceListPage> cut = Render<InvoiceListPage>();

        // Assert - Paid and Cancelled invoices have View action (menu visible)
        cut.Markup.ShouldContain("more_vert");
    }

    [Fact]
    public void ActionsMenu_VisibleForSentInvoices()
    {
        // Arrange
        FakeInvoiceQueryService fakeQueryService = new();
        FakeInvoiceCommandService fakeCommandService = new();
        fakeQueryService.SetInvoices(
            new List<InvoiceListItemDto>
            {
                CreateDto(
                    1,
                    "INV-00001",
                    "Sent Customer",
                    FixedToday,
                    FixedToday.AddDays(30),
                    InvoiceStatus.Sent,
                    false,
                    1,
                    100m
                ),
            }
        );
        Services.AddSingleton<IInvoiceQueryService>(fakeQueryService);
        Services.AddSingleton<IInvoiceCommandService>(fakeCommandService);

        // Act
        IRenderedComponent<InvoiceListPage> cut = Render<InvoiceListPage>();

        // Assert - Sent invoices have status change actions, so menu is visible
        cut.Markup.ShouldContain("more_vert");
    }

    [Fact]
    public void ActionsMenu_VisibleForDraftAndSentInMixedList()
    {
        // Arrange
        FakeInvoiceQueryService fakeQueryService = new();
        FakeInvoiceCommandService fakeCommandService = new();
        fakeQueryService.SetInvoices(
            new List<InvoiceListItemDto>
            {
                CreateDto(
                    1,
                    "INV-00001",
                    "Draft Customer",
                    FixedToday,
                    FixedToday.AddDays(30),
                    InvoiceStatus.Draft,
                    false,
                    1,
                    100m
                ),
                CreateDto(
                    2,
                    "INV-00002",
                    "Sent Customer",
                    FixedToday,
                    FixedToday.AddDays(30),
                    InvoiceStatus.Sent,
                    false,
                    1,
                    200m
                ),
                CreateDto(
                    3,
                    "INV-00003",
                    "Paid Customer",
                    FixedToday,
                    FixedToday.AddDays(30),
                    InvoiceStatus.Paid,
                    false,
                    1,
                    300m
                ),
                CreateDto(
                    4,
                    "INV-00004",
                    "Cancelled Customer",
                    FixedToday,
                    FixedToday.AddDays(30),
                    InvoiceStatus.Cancelled,
                    false,
                    1,
                    400m
                ),
            }
        );
        Services.AddSingleton<IInvoiceQueryService>(fakeQueryService);
        Services.AddSingleton<IInvoiceCommandService>(fakeCommandService);

        // Act
        IRenderedComponent<InvoiceListPage> cut = Render<InvoiceListPage>();

        // Assert - Draft and Sent have actions menus with more_vert icons
        cut.Markup.ShouldContain("more_vert");
    }

    [Fact]
    public void MixedStatuses_AllInvoicesHaveActionMenus()
    {
        // Arrange
        FakeInvoiceQueryService fakeQueryService = new();
        FakeInvoiceCommandService fakeCommandService = new();
        fakeQueryService.SetInvoices(
            new List<InvoiceListItemDto>
            {
                CreateDto(
                    1,
                    "INV-00001",
                    "Draft Customer",
                    FixedToday,
                    FixedToday.AddDays(30),
                    InvoiceStatus.Draft,
                    false,
                    1,
                    100m
                ),
                CreateDto(
                    2,
                    "INV-00002",
                    "Sent Customer",
                    FixedToday,
                    FixedToday.AddDays(30),
                    InvoiceStatus.Sent,
                    false,
                    1,
                    200m
                ),
                CreateDto(
                    3,
                    "INV-00003",
                    "Paid Customer",
                    FixedToday,
                    FixedToday.AddDays(30),
                    InvoiceStatus.Paid,
                    false,
                    1,
                    300m
                ),
                CreateDto(
                    4,
                    "INV-00004",
                    "Cancelled Customer",
                    FixedToday,
                    FixedToday.AddDays(30),
                    InvoiceStatus.Cancelled,
                    false,
                    1,
                    400m
                ),
            }
        );
        Services.AddSingleton<IInvoiceQueryService>(fakeQueryService);
        Services.AddSingleton<IInvoiceCommandService>(fakeCommandService);

        // Act
        IRenderedComponent<InvoiceListPage> cut = Render<InvoiceListPage>();

        // Assert - All invoices have action menus (at least View action)
        // ActionsMenu renders with actions-button class
        IReadOnlyList<AngleSharp.Dom.IElement> actionButtons = cut.FindAll(".actions-button");
        actionButtons.Count.ShouldBe(4); // All 4 invoices have action menus

        // All 4 statuses should be displayed as badges
        IReadOnlyList<AngleSharp.Dom.IElement> badges = cut.FindAll(".rz-badge");
        badges.Count.ShouldBe(4);
    }
}
