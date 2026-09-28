using InvoiceApp.Domain;
using InvoiceApp.Features.Customers.Dtos;
using InvoiceApp.Tests.TestSupport;
using Shouldly;

namespace InvoiceApp.Tests.Feature;

public sealed class CustomerServiceTests
{
    [Fact]
    public async Task Create_ValidForm_SavesAllFieldsCorrectly()
    {
        // Arrange
        await using CustomerTestHelper helper = await CustomerTestHelper.CreateAsync();

        CustomerFormDto form = new(
            Id: null,
            Name: "John Doe",
            Email: "john@example.com",
            Phone: "555-1234",
            CompanyName: "Acme Corp",
            Address: "123 Main St"
        );

        // Act
        Result<int> result = await helper.CustomerService.CreateAsync(form);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        int customerId = result.Value;

        Customer? reloaded = await helper.ReloadCustomerAsync(customerId);
        reloaded.ShouldNotBeNull();
        reloaded.Name.ShouldBe("John Doe");
        reloaded.Email.ShouldBe("john@example.com");
        reloaded.Phone.ShouldBe("555-1234");
        reloaded.CompanyName.ShouldBe("Acme Corp");
        reloaded.Address.ShouldBe("123 Main St");
    }

    [Fact]
    public async Task Create_TrimsWhitespace_FromAllFields()
    {
        // Arrange
        await using CustomerTestHelper helper = await CustomerTestHelper.CreateAsync();

        CustomerFormDto form = new(
            Id: null,
            Name: "  John Doe  ",
            Email: "  john@example.com  ",
            Phone: "  555-1234  ",
            CompanyName: "  Acme Corp  ",
            Address: "  123 Main St  "
        );

        // Act
        Result<int> result = await helper.CustomerService.CreateAsync(form);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Customer? reloaded = await helper.ReloadCustomerAsync(result.Value);
        reloaded.ShouldNotBeNull();
        reloaded.Name.ShouldBe("John Doe");
        reloaded.Email.ShouldBe("john@example.com");
        reloaded.Phone.ShouldBe("555-1234");
        reloaded.CompanyName.ShouldBe("Acme Corp");
        reloaded.Address.ShouldBe("123 Main St");
    }

    [Fact]
    public async Task Create_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        await using CustomerTestHelper helper = await CustomerTestHelper.CreateAsync();
        await helper.SeedCustomerAsync(email: "existing@example.com");

        CustomerFormDto form = CustomerTestHelper.CreateValidForm(email: "existing@example.com");

        // Act
        Result<int> result = await helper.CustomerService.CreateAsync(form);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldBe("A customer with this email already exists.");
    }

    [Fact]
    public async Task Create_DuplicateEmailCaseInsensitive_ReturnsFailure()
    {
        // Arrange
        await using CustomerTestHelper helper = await CustomerTestHelper.CreateAsync();
        await helper.SeedCustomerAsync(email: "existing@example.com");

        CustomerFormDto form = CustomerTestHelper.CreateValidForm(email: "EXISTING@EXAMPLE.COM");

        // Act
        Result<int> result = await helper.CustomerService.CreateAsync(form);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldBe("A customer with this email already exists.");
    }

    [Fact]
    public async Task Create_InvalidForm_ReturnsFailure_NothingSaved()
    {
        // Arrange
        await using CustomerTestHelper helper = await CustomerTestHelper.CreateAsync();

        CustomerFormDto form = new(
            Id: null,
            Name: "", // Invalid: empty
            Email: "john@example.com",
            Phone: "555-1234",
            CompanyName: null,
            Address: null
        );

        // Act
        Result<int> result = await helper.CustomerService.CreateAsync(form);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Name is required");

        int count = await helper.CountCustomersAsync();
        count.ShouldBe(0);
    }

    [Fact]
    public async Task Update_ValidForm_UpdatesAllFields()
    {
        // Arrange
        await using CustomerTestHelper helper = await CustomerTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync(
            name: "Original Name",
            email: "original@example.com",
            phone: "555-0000"
        );

        CustomerFormDto form = new(
            Id: customerId,
            Name: "Updated Name",
            Email: "updated@example.com",
            Phone: "555-9999",
            CompanyName: "New Company",
            Address: "New Address"
        );

        // Act
        Result result = await helper.CustomerService.UpdateAsync(customerId, form);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Customer? reloaded = await helper.ReloadCustomerAsync(customerId);
        reloaded.ShouldNotBeNull();
        reloaded.Name.ShouldBe("Updated Name");
        reloaded.Email.ShouldBe("updated@example.com");
        reloaded.Phone.ShouldBe("555-9999");
        reloaded.CompanyName.ShouldBe("New Company");
        reloaded.Address.ShouldBe("New Address");
    }

    [Fact]
    public async Task Update_NonExistent_ReturnsFailure()
    {
        // Arrange
        await using CustomerTestHelper helper = await CustomerTestHelper.CreateAsync();

        CustomerFormDto form = CustomerTestHelper.CreateValidForm(id: 999);

        // Act
        Result result = await helper.CustomerService.UpdateAsync(999, form);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldBe("Customer not found.");
    }

    [Fact]
    public async Task Update_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        await using CustomerTestHelper helper = await CustomerTestHelper.CreateAsync();
        await helper.SeedCustomerAsync(email: "other@example.com");
        int customerId = await helper.SeedCustomerAsync(email: "original@example.com");

        CustomerFormDto form = CustomerTestHelper.CreateValidForm(
            id: customerId,
            email: "other@example.com"
        );

        // Act
        Result result = await helper.CustomerService.UpdateAsync(customerId, form);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldBe("A customer with this email already exists.");
    }

    [Fact]
    public async Task Update_SameEmailSameCustomer_ReturnsSuccess()
    {
        // Arrange
        await using CustomerTestHelper helper = await CustomerTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync(email: "same@example.com");

        CustomerFormDto form = new(
            Id: customerId,
            Name: "Updated Name",
            Email: "same@example.com", // Same email, should be allowed
            Phone: "555-1111",
            CompanyName: null,
            Address: null
        );

        // Act
        Result result = await helper.CustomerService.UpdateAsync(customerId, form);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Update_InvalidForm_ReturnsFailure()
    {
        // Arrange
        await using CustomerTestHelper helper = await CustomerTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        CustomerFormDto form = new(
            Id: customerId,
            Name: "", // Invalid
            Email: "john@example.com",
            Phone: "555-1234",
            CompanyName: null,
            Address: null
        );

        // Act
        Result result = await helper.CustomerService.UpdateAsync(customerId, form);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Name is required");
    }

    [Fact]
    public async Task Delete_CustomerWithNoInvoices_Succeeds()
    {
        // Arrange
        await using CustomerTestHelper helper = await CustomerTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync();

        // Act
        Result result = await helper.CustomerService.DeleteAsync(customerId);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Customer? reloaded = await helper.ReloadCustomerAsync(customerId);
        reloaded.ShouldBeNull();
    }

    [Fact]
    public async Task Delete_CustomerWithInvoices_ReturnsFailureWithCount()
    {
        // Arrange
        await using CustomerTestHelper helper = await CustomerTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerWithInvoiceAsync();

        int invoiceCount = await helper.GetInvoiceCountAsync(customerId);
        invoiceCount.ShouldBe(1);

        // Act
        Result result = await helper.CustomerService.DeleteAsync(customerId);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldBe("This customer has 1 invoice(s) and can't be deleted.");

        // Customer should still exist
        Customer? reloaded = await helper.ReloadCustomerAsync(customerId);
        reloaded.ShouldNotBeNull();
    }

    [Fact]
    public async Task Delete_NonExistent_ReturnsFailure()
    {
        // Arrange
        await using CustomerTestHelper helper = await CustomerTestHelper.CreateAsync();

        // Act
        Result result = await helper.CustomerService.DeleteAsync(999);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldBe("Customer not found.");
    }

    [Fact]
    public async Task GetDetails_ExistingCustomer_ReturnsCorrectData()
    {
        // Arrange
        await using CustomerTestHelper helper = await CustomerTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerAsync(
            name: "John Doe",
            email: "john@example.com",
            phone: "555-1234",
            companyName: "Acme Corp",
            address: "123 Main St"
        );

        // Act
        CustomerDetailsDto? details = await helper.CustomerService.GetDetailsAsync(customerId);

        // Assert
        details.ShouldNotBeNull();
        details.Id.ShouldBe(customerId);
        details.Name.ShouldBe("John Doe");
        details.Email.ShouldBe("john@example.com");
        details.Phone.ShouldBe("555-1234");
        details.CompanyName.ShouldBe("Acme Corp");
        details.Address.ShouldBe("123 Main St");
        details.InvoiceCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetDetails_CustomerWithInvoices_ReturnsCorrectInvoiceCount()
    {
        // Arrange
        await using CustomerTestHelper helper = await CustomerTestHelper.CreateAsync();
        int customerId = await helper.SeedCustomerWithInvoiceAsync();

        // Act
        CustomerDetailsDto? details = await helper.CustomerService.GetDetailsAsync(customerId);

        // Assert
        details.ShouldNotBeNull();
        details.InvoiceCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetDetails_NonExistent_ReturnsNull()
    {
        // Arrange
        await using CustomerTestHelper helper = await CustomerTestHelper.CreateAsync();

        // Act
        CustomerDetailsDto? details = await helper.CustomerService.GetDetailsAsync(999);

        // Assert
        details.ShouldBeNull();
    }
}
