using FluentValidation.TestHelper;
using InvoiceApp.Features.Account.Dtos;
using InvoiceApp.Features.Account.Validators;

namespace InvoiceApp.Tests.Unit;

public sealed class ChangeEmailDtoValidatorTests
{
    private readonly ChangeEmailDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_When_EmailIsValid()
    {
        // Arrange
        ChangeEmailDto dto = new(NewEmail: "user@example.com");

        // Act
        TestValidationResult<ChangeEmailDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Fail_When_EmailIsEmptyOrWhitespace(string email)
    {
        // Arrange
        ChangeEmailDto dto = new(NewEmail: email);

        // Act
        TestValidationResult<ChangeEmailDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.NewEmail)
            .WithErrorMessage("Email is required.");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("invalid@")]
    [InlineData("@nodomain.com")]
    public void Should_Fail_When_EmailIsInvalidFormat(string email)
    {
        // Arrange
        ChangeEmailDto dto = new(NewEmail: email);

        // Act
        TestValidationResult<ChangeEmailDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.NewEmail)
            .WithErrorMessage("Email must be a valid email address.");
    }

    [Fact]
    public void Should_Fail_When_EmailExceedsMaxLength()
    {
        // Arrange
        string longEmail = new string('a', 245) + "@example.com"; // 257 chars total
        ChangeEmailDto dto = new(NewEmail: longEmail);

        // Act
        TestValidationResult<ChangeEmailDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.NewEmail)
            .WithErrorMessage("Email must not exceed 256 characters.");
    }
}
