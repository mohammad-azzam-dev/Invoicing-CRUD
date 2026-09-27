using FluentValidation.TestHelper;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Validators;

namespace InvoiceApp.Tests.Unit;

public sealed class InvoiceFormDtoValidatorTests
{
    private readonly InvoiceFormDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_When_AllFieldsAreValid()
    {
        // Arrange
        InvoiceFormDto dto = new(
            CustomerId: 1,
            IssueDate: DateOnly.FromDateTime(DateTime.Today),
            DueDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            TaxRate: 10m
        );

        // Act
        TestValidationResult<InvoiceFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_CustomerIdIsZero()
    {
        // Arrange
        InvoiceFormDto dto = new(
            CustomerId: 0,
            IssueDate: DateOnly.FromDateTime(DateTime.Today),
            DueDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            TaxRate: 10m
        );

        // Act
        TestValidationResult<InvoiceFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public void Should_Fail_When_CustomerIdIsNegative()
    {
        // Arrange
        InvoiceFormDto dto = new(
            CustomerId: -1,
            IssueDate: DateOnly.FromDateTime(DateTime.Today),
            DueDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            TaxRate: 10m
        );

        // Act
        TestValidationResult<InvoiceFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public void Should_Fail_When_DueDateIsBeforeIssueDate()
    {
        // Arrange
        InvoiceFormDto dto = new(
            CustomerId: 1,
            IssueDate: DateOnly.FromDateTime(DateTime.Today),
            DueDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            TaxRate: 10m
        );

        // Act
        TestValidationResult<InvoiceFormDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.DueDate)
            .WithErrorMessage("Due date must be on or after the issue date.");
    }

    [Fact]
    public void Should_Pass_When_DueDateEqualsIssueDate()
    {
        // Arrange
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        InvoiceFormDto dto = new(CustomerId: 1, IssueDate: today, DueDate: today, TaxRate: 10m);

        // Act
        TestValidationResult<InvoiceFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DueDate);
    }

    [Fact]
    public void Should_Fail_When_TaxRateIsNegative()
    {
        // Arrange
        InvoiceFormDto dto = new(
            CustomerId: 1,
            IssueDate: DateOnly.FromDateTime(DateTime.Today),
            DueDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            TaxRate: -1m
        );

        // Act
        TestValidationResult<InvoiceFormDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.TaxRate)
            .WithErrorMessage("Tax rate must be between 0 and 100.");
    }

    [Fact]
    public void Should_Fail_When_TaxRateExceeds100()
    {
        // Arrange
        InvoiceFormDto dto = new(
            CustomerId: 1,
            IssueDate: DateOnly.FromDateTime(DateTime.Today),
            DueDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            TaxRate: 101m
        );

        // Act
        TestValidationResult<InvoiceFormDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.TaxRate)
            .WithErrorMessage("Tax rate must be between 0 and 100.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Should_Pass_When_TaxRateIsBetween0And100(decimal taxRate)
    {
        // Arrange
        InvoiceFormDto dto = new(
            CustomerId: 1,
            IssueDate: DateOnly.FromDateTime(DateTime.Today),
            DueDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            TaxRate: taxRate
        );

        // Act
        TestValidationResult<InvoiceFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TaxRate);
    }
}
