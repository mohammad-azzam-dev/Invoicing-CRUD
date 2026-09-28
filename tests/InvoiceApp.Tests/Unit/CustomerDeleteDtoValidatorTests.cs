using FluentValidation.TestHelper;
using InvoiceApp.Domain;
using InvoiceApp.Features.Customers.Dtos;
using InvoiceApp.Features.Customers.Validators;
using InvoiceApp.Tests.TestSupport;

namespace InvoiceApp.Tests.Unit;

public sealed class CustomerDeleteDtoValidatorTests
{
    private readonly FakeCustomerRepository _repository = new();
    private readonly CustomerDeleteDtoValidator _validator;

    public CustomerDeleteDtoValidatorTests()
    {
        _validator = new CustomerDeleteDtoValidator(_repository);
    }

    [Fact]
    public async Task Should_Pass_When_CustomerExistsWithNoInvoices()
    {
        // Arrange
        Customer customer = Customer.Create("John Doe", "555-1234", "john@example.com");
        typeof(Customer).GetProperty("Id")!.SetValue(customer, 1);
        _repository.SetCustomerEntity(customer, invoiceCount: 0);

        CustomerDeleteDto dto = new(Id: 1);

        // Act
        TestValidationResult<CustomerDeleteDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Should_Fail_When_CustomerDoesNotExist()
    {
        // Arrange
        CustomerDeleteDto dto = new(Id: 999);

        // Act
        TestValidationResult<CustomerDeleteDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id).WithErrorMessage("Customer not found.");
    }

    [Fact]
    public async Task Should_Fail_When_CustomerHasInvoices()
    {
        // Arrange
        Customer customer = Customer.Create("John Doe", "555-1234", "john@example.com");
        typeof(Customer).GetProperty("Id")!.SetValue(customer, 1);
        _repository.SetCustomerEntity(customer, invoiceCount: 3);

        CustomerDeleteDto dto = new(Id: 1);

        // Act
        TestValidationResult<CustomerDeleteDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("This customer has 3 invoice(s) and can't be deleted.");
    }

    [Fact]
    public async Task Should_Fail_When_CustomerHasOneInvoice()
    {
        // Arrange
        Customer customer = Customer.Create("John Doe", "555-1234", "john@example.com");
        typeof(Customer).GetProperty("Id")!.SetValue(customer, 1);
        _repository.SetCustomerEntity(customer, invoiceCount: 1);

        CustomerDeleteDto dto = new(Id: 1);

        // Act
        TestValidationResult<CustomerDeleteDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("This customer has 1 invoice(s) and can't be deleted.");
    }
}
