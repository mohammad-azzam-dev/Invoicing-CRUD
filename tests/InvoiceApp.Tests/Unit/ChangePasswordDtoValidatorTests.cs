using FluentValidation.TestHelper;
using InvoiceApp.Features.Account.Dtos;
using InvoiceApp.Features.Account.Validators;

namespace InvoiceApp.Tests.Unit;

public sealed class ChangePasswordDtoValidatorTests
{
    private readonly ChangePasswordDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_When_AllFieldsAreValid()
    {
        // Arrange
        ChangePasswordDto dto = new(
            CurrentPassword: "oldpassword",
            NewPassword: "newpassword123",
            ConfirmPassword: "newpassword123"
        );

        // Act
        TestValidationResult<ChangePasswordDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Fail_When_CurrentPasswordIsEmpty(string password)
    {
        // Arrange
        ChangePasswordDto dto = new(
            CurrentPassword: password,
            NewPassword: "newpassword123",
            ConfirmPassword: "newpassword123"
        );

        // Act
        TestValidationResult<ChangePasswordDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.CurrentPassword)
            .WithErrorMessage("Current password is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Fail_When_NewPasswordIsEmpty(string password)
    {
        // Arrange
        ChangePasswordDto dto = new(
            CurrentPassword: "oldpassword",
            NewPassword: password,
            ConfirmPassword: password
        );

        // Act
        TestValidationResult<ChangePasswordDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Password is required.");
    }

    [Fact]
    public void Should_Fail_When_NewPasswordIsTooShort()
    {
        // Arrange
        ChangePasswordDto dto = new(
            CurrentPassword: "oldpassword",
            NewPassword: "short",
            ConfirmPassword: "short"
        );

        // Act
        TestValidationResult<ChangePasswordDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Password must be at least 8 characters.");
    }

    [Fact]
    public void Should_Fail_When_ConfirmPasswordDoesNotMatch()
    {
        // Arrange
        ChangePasswordDto dto = new(
            CurrentPassword: "oldpassword",
            NewPassword: "newpassword123",
            ConfirmPassword: "differentpassword"
        );

        // Act
        TestValidationResult<ChangePasswordDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("Passwords do not match.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Fail_When_ConfirmPasswordIsEmpty(string password)
    {
        // Arrange
        ChangePasswordDto dto = new(
            CurrentPassword: "oldpassword",
            NewPassword: "newpassword123",
            ConfirmPassword: password
        );

        // Act
        TestValidationResult<ChangePasswordDto> result = _validator.TestValidate(dto);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("Please confirm the password.");
    }
}
