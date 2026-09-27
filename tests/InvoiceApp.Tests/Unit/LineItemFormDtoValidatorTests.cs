using FluentValidation.TestHelper;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Validators;

namespace InvoiceApp.Tests.Unit;

public sealed class LineItemFormDtoValidatorTests
{
    private readonly LineItemFormDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_When_AllFieldsAreValid()
    {
        // Arrange
        LineItemFormDto dto = new(
            Id: 0,
            Description: "Widget",
            Quantity: 2m,
            UnitPrice: 10m,
            DiscountPercent: 5m
        );

        // Act
        TestValidationResult<LineItemFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Fail_When_DescriptionIsEmpty(string? description)
    {
        // Arrange
        LineItemFormDto dto = new(
            Id: 0,
            Description: description!,
            Quantity: 2m,
            UnitPrice: 10m,
            DiscountPercent: 0m
        );

        // Act
        TestValidationResult<LineItemFormDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void Should_Fail_When_DescriptionExceeds200Characters()
    {
        // Arrange
        string longDescription = new('a', 201);
        LineItemFormDto dto = new(
            Id: 0,
            Description: longDescription,
            Quantity: 2m,
            UnitPrice: 10m,
            DiscountPercent: 0m
        );

        // Act
        TestValidationResult<LineItemFormDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 200 characters.");
    }

    [Fact]
    public void Should_Pass_When_DescriptionIsExactly200Characters()
    {
        // Arrange
        string description = new('a', 200);
        LineItemFormDto dto = new(
            Id: 0,
            Description: description,
            Quantity: 2m,
            UnitPrice: 10m,
            DiscountPercent: 0m
        );

        // Act
        TestValidationResult<LineItemFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Should_Fail_When_QuantityIsZeroOrNegative(decimal quantity)
    {
        // Arrange
        LineItemFormDto dto = new(
            Id: 0,
            Description: "Widget",
            Quantity: quantity,
            UnitPrice: 10m,
            DiscountPercent: 0m
        );

        // Act
        TestValidationResult<LineItemFormDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Quantity)
            .WithErrorMessage("Quantity must be greater than 0.");
    }

    [Fact]
    public void Should_Fail_When_QuantityHasMoreThan2DecimalPlaces()
    {
        // Arrange
        LineItemFormDto dto = new(
            Id: 0,
            Description: "Widget",
            Quantity: 1.234m,
            UnitPrice: 10m,
            DiscountPercent: 0m
        );

        // Act
        TestValidationResult<LineItemFormDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Quantity)
            .WithErrorMessage("Quantity cannot have more than 2 decimal places.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1.5)]
    [InlineData(1.55)]
    [InlineData(0.01)]
    public void Should_Pass_When_QuantityHasValidDecimalPlaces(decimal quantity)
    {
        // Arrange
        LineItemFormDto dto = new(
            Id: 0,
            Description: "Widget",
            Quantity: quantity,
            UnitPrice: 10m,
            DiscountPercent: 0m
        );

        // Act
        TestValidationResult<LineItemFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Should_Fail_When_UnitPriceIsNegative()
    {
        // Arrange
        LineItemFormDto dto = new(
            Id: 0,
            Description: "Widget",
            Quantity: 2m,
            UnitPrice: -1m,
            DiscountPercent: 0m
        );

        // Act
        TestValidationResult<LineItemFormDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.UnitPrice)
            .WithErrorMessage("Unit price cannot be negative.");
    }

    [Fact]
    public void Should_Pass_When_UnitPriceIsZero()
    {
        // Arrange
        LineItemFormDto dto = new(
            Id: 0,
            Description: "Complimentary item",
            Quantity: 1m,
            UnitPrice: 0m,
            DiscountPercent: 0m
        );

        // Act
        TestValidationResult<LineItemFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UnitPrice);
    }

    [Fact]
    public void Should_Fail_When_UnitPriceHasMoreThan2DecimalPlaces()
    {
        // Arrange
        LineItemFormDto dto = new(
            Id: 0,
            Description: "Widget",
            Quantity: 2m,
            UnitPrice: 10.123m,
            DiscountPercent: 0m
        );

        // Act
        TestValidationResult<LineItemFormDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.UnitPrice)
            .WithErrorMessage("Unit price cannot have more than 2 decimal places.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Should_Fail_When_DiscountPercentIsNegative(decimal discountPercent)
    {
        // Arrange
        LineItemFormDto dto = new(
            Id: 0,
            Description: "Widget",
            Quantity: 2m,
            UnitPrice: 10m,
            DiscountPercent: discountPercent
        );

        // Act
        TestValidationResult<LineItemFormDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.DiscountPercent)
            .WithErrorMessage("Discount must be between 0 and 100.");
    }

    [Theory]
    [InlineData(101)]
    [InlineData(100.01)]
    public void Should_Fail_When_DiscountPercentExceeds100(decimal discountPercent)
    {
        // Arrange
        LineItemFormDto dto = new(
            Id: 0,
            Description: "Widget",
            Quantity: 2m,
            UnitPrice: 10m,
            DiscountPercent: discountPercent
        );

        // Act
        TestValidationResult<LineItemFormDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.DiscountPercent)
            .WithErrorMessage("Discount must be between 0 and 100.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Should_Pass_When_DiscountPercentIsBetween0And100(decimal discountPercent)
    {
        // Arrange
        LineItemFormDto dto = new(
            Id: 0,
            Description: "Widget",
            Quantity: 2m,
            UnitPrice: 10m,
            DiscountPercent: discountPercent
        );

        // Act
        TestValidationResult<LineItemFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DiscountPercent);
    }
}
