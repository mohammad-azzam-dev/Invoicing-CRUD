using Bunit;
using InvoiceApp.Features.Customers.Dtos;
using InvoiceApp.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using CustomerListPage = InvoiceApp.Components.Pages.Customers.Index;

namespace InvoiceApp.Tests.Feature;

public sealed class CustomerListComponentTests : BunitContext
{
    public CustomerListComponentTests()
    {
        Services.AddRadzenComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void EmptyList_RendersNoCustomersMessage()
    {
        // Arrange
        FakeCustomerService fakeService = new();
        fakeService.SetCustomers([]);
        Services.AddSingleton<InvoiceApp.Features.Customers.Interfaces.ICustomerService>(
            fakeService
        );

        // Act
        IRenderedComponent<CustomerListPage> cut = Render<CustomerListPage>();

        // Assert
        cut.Markup.ShouldContain("No customers found");
    }

    [Fact]
    public void WithCustomers_RendersCustomerData()
    {
        // Arrange
        FakeCustomerService fakeService = new();
        fakeService.SetCustomers([
            new CustomerListItemDto(
                1,
                "Acme Corp",
                "John Doe",
                "Acme Corp",
                "john@acme.com",
                "555-1234",
                3
            ),
            new CustomerListItemDto(
                2,
                "Jane Smith",
                "Jane Smith",
                null,
                "jane@example.com",
                "555-5678",
                0
            ),
        ]);
        Services.AddSingleton<InvoiceApp.Features.Customers.Interfaces.ICustomerService>(
            fakeService
        );

        // Act
        IRenderedComponent<CustomerListPage> cut = Render<CustomerListPage>();

        // Assert
        cut.Markup.ShouldContain("Acme Corp");
        cut.Markup.ShouldContain("John Doe");
        cut.Markup.ShouldContain("jane@example.com");
    }

    [Fact]
    public void NewCustomerButton_IsPresent()
    {
        // Arrange
        FakeCustomerService fakeService = new();
        fakeService.SetCustomers([]);
        Services.AddSingleton<InvoiceApp.Features.Customers.Interfaces.ICustomerService>(
            fakeService
        );

        // Act
        IRenderedComponent<CustomerListPage> cut = Render<CustomerListPage>();

        // Assert
        cut.Markup.ShouldContain("New Customer");
    }

    [Fact]
    public void PageTitle_IsCorrect()
    {
        // Arrange
        FakeCustomerService fakeService = new();
        fakeService.SetCustomers([]);
        Services.AddSingleton<InvoiceApp.Features.Customers.Interfaces.ICustomerService>(
            fakeService
        );

        // Act
        IRenderedComponent<CustomerListPage> cut = Render<CustomerListPage>();

        // Assert
        cut.Markup.ShouldContain("Customers");
    }

    [Fact]
    public void SearchBox_IsPresent()
    {
        // Arrange
        FakeCustomerService fakeService = new();
        fakeService.SetCustomers([]);
        Services.AddSingleton<InvoiceApp.Features.Customers.Interfaces.ICustomerService>(
            fakeService
        );

        // Act
        IRenderedComponent<CustomerListPage> cut = Render<CustomerListPage>();

        // Assert
        cut.Markup.ShouldContain("Search by name, company, email, or phone");
    }
}
