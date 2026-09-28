using FluentValidation.TestHelper;
using InvoiceApp.Features.Customers.Dtos;
using InvoiceApp.Features.Customers.Validators;
using InvoiceApp.Tests.TestSupport;

namespace InvoiceApp.Tests.Unit;

public sealed class CustomerFormDtoValidatorTests
{
    private readonly FakeCustomerRepository _repository = new();
    private readonly CustomerFormDtoValidator _validator;

    public CustomerFormDtoValidatorTests()
    {
        _validator = new CustomerFormDtoValidator(_repository);
    }

    [Fact]
    public async Task Should_Pass_When_AllFieldsAreValid()
    {
        // Arrange
        CustomerFormDto dto = new(
            Id: null,
            Name: "John Doe",
            Email: "john@example.com",
            Phone: "555-1234",
            CompanyName: "Acme Corp",
            Address: "123 Main St"
        );

        // Act
        TestValidationResult<CustomerFormDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Should_Pass_When_OptionalFieldsAreNull()
    {
        // Arrange
        CustomerFormDto dto = new(
            Id: null,
            Name: "John Doe",
            Email: "john@example.com",
            Phone: "555-1234",
            CompanyName: null,
            Address: null
        );

        // Act
        TestValidationResult<CustomerFormDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_Fail_When_NameIsEmptyOrWhitespace(string name)
    {
        // Arrange
        CustomerFormDto dto = new(
            Id: null,
            Name: name,
            Email: "john@example.com",
            Phone: "555-1234",
            CompanyName: null,
            Address: null
        );

        // Act
        TestValidationResult<CustomerFormDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name is required.");
    }

    [Fact]
    public async Task Should_Fail_When_NameExceedsMaxLength()
    {
        // Arrange
        string longName = new('a', 101);
        CustomerFormDto dto = new(
            Id: null,
            Name: longName,
            Email: "john@example.com",
            Phone: "555-1234",
            CompanyName: null,
            Address: null
        );

        // Act
        TestValidationResult<CustomerFormDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must not exceed 100 characters.");
    }

    [Fact]
    public async Task Should_Pass_When_NameIsAtMaxLength()
    {
        // Arrange
        string maxName = new('a', 100);
        CustomerFormDto dto = new(
            Id: null,
            Name: maxName,
            Email: "john@example.com",
            Phone: "555-1234",
            CompanyName: null,
            Address: null
        );

        // Act
        TestValidationResult<CustomerFormDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_Fail_When_EmailIsEmptyOrWhitespace(string email)
    {
        // Arrange
        CustomerFormDto dto = new(
            Id: null,
            Name: "John Doe",
            Email: email,
            Phone: "555-1234",
            CompanyName: null,
            Address: null
        );

        // Act
        TestValidationResult<CustomerFormDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required.");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("invalid@")]
    [InlineData("@nodomain.com")]
    public async Task Should_Fail_When_EmailIsInvalidFormat(string email)
    {
        // Arrange
        CustomerFormDto dto = new(
            Id: null,
            Name: "John Doe",
            Email: email,
            Phone: "555-1234",
            CompanyName: null,
            Address: null
        );

        // Act
        TestValidationResult<CustomerFormDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must be a valid email address.");
    }

    [Fact]
    public async Task Should_Fail_When_EmailExceedsMaxLength()
    {
        // Arrange
        string longEmail = new string('a', 140) + "@example.com"; // 152 chars total
        CustomerFormDto dto = new(
            Id: null,
            Name: "John Doe",
            Email: longEmail,
            Phone: "555-1234",
            CompanyName: null,
            Address: null
        );

        // Act
        TestValidationResult<CustomerFormDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must not exceed 150 characters.");
    }

    [Fact]
    public async Task Should_Fail_When_EmailAlreadyExists()
    {
        // Arrange
        _repository.AddEmail("existing@example.com");
        CustomerFormDto dto = new(
            Id: null,
            Name: "John Doe",
            Email: "existing@example.com",
            Phone: "555-1234",
            CompanyName: null,
            Address: null
        );

        // Act
        TestValidationResult<CustomerFormDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("A customer with this email already exists.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_Fail_When_PhoneIsEmptyOrWhitespace(string phone)
    {
        // Arrange
        CustomerFormDto dto = new(
            Id: null,
            Name: "John Doe",
            Email: "john@example.com",
            Phone: phone,
            CompanyName: null,
            Address: null
        );

        // Act
        TestValidationResult<CustomerFormDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("Phone is required.");
    }

    [Fact]
    public async Task Should_Fail_When_PhoneExceedsMaxLength()
    {
        // Arrange
        string longPhone = new('1', 31);
        CustomerFormDto dto = new(
            Id: null,
            Name: "John Doe",
            Email: "john@example.com",
            Phone: longPhone,
            CompanyName: null,
            Address: null
        );

        // Act
        TestValidationResult<CustomerFormDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("Phone must not exceed 30 characters.");
    }

    [Fact]
    public async Task Should_Pass_When_PhoneIsAtMaxLength()
    {
        // Arrange
        string maxPhone = new('1', 30);
        CustomerFormDto dto = new(
            Id: null,
            Name: "John Doe",
            Email: "john@example.com",
            Phone: maxPhone,
            CompanyName: null,
            Address: null
        );

        // Act
        TestValidationResult<CustomerFormDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public async Task Should_Fail_When_CompanyNameExceedsMaxLength()
    {
        // Arrange
        string longCompany = new('a', 151);
        CustomerFormDto dto = new(
            Id: null,
            Name: "John Doe",
            Email: "john@example.com",
            Phone: "555-1234",
            CompanyName: longCompany,
            Address: null
        );

        // Act
        TestValidationResult<CustomerFormDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CompanyName)
            .WithErrorMessage("Company name must not exceed 150 characters.");
    }

    [Fact]
    public async Task Should_Pass_When_CompanyNameIsAtMaxLength()
    {
        // Arrange
        string maxCompany = new('a', 150);
        CustomerFormDto dto = new(
            Id: null,
            Name: "John Doe",
            Email: "john@example.com",
            Phone: "555-1234",
            CompanyName: maxCompany,
            Address: null
        );

        // Act
        TestValidationResult<CustomerFormDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CompanyName);
    }

    [Fact]
    public async Task Should_Fail_When_AddressExceedsMaxLength()
    {
        // Arrange
        string longAddress = new('a', 301);
        CustomerFormDto dto = new(
            Id: null,
            Name: "John Doe",
            Email: "john@example.com",
            Phone: "555-1234",
            CompanyName: null,
            Address: longAddress
        );

        // Act
        TestValidationResult<CustomerFormDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address)
            .WithErrorMessage("Address must not exceed 300 characters.");
    }

    [Fact]
    public async Task Should_Pass_When_AddressIsAtMaxLength()
    {
        // Arrange
        string maxAddress = new('a', 300);
        CustomerFormDto dto = new(
            Id: null,
            Name: "John Doe",
            Email: "john@example.com",
            Phone: "555-1234",
            CompanyName: null,
            Address: maxAddress
        );

        // Act
        TestValidationResult<CustomerFormDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Address);
    }
}
