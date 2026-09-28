using InvoiceApp.Domain;
using InvoiceApp.Features.Account.Dtos;
using InvoiceApp.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace InvoiceApp.Tests.Feature;

public sealed class ProfileServiceTests
{
    [Fact]
    public async Task GetEmailAsync_ExistingUser_ReturnsEmail()
    {
        // Arrange
        await using ProfileTestHelper helper = await ProfileTestHelper.CreateAsync();
        string userId = await helper.SeedUserAsync("test@example.com");

        // Act
        string? email = await helper.ProfileService.GetEmailAsync(userId);

        // Assert
        email.ShouldBe("test@example.com");
    }

    [Fact]
    public async Task GetEmailAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        await using ProfileTestHelper helper = await ProfileTestHelper.CreateAsync();

        // Act
        string? email = await helper.ProfileService.GetEmailAsync("non-existent-id");

        // Assert
        email.ShouldBeNull();
    }

    [Fact]
    public async Task ChangeEmailAsync_ValidEmail_UpdatesEmail()
    {
        // Arrange
        await using ProfileTestHelper helper = await ProfileTestHelper.CreateAsync();
        string userId = await helper.SeedUserAsync("original@example.com");

        ChangeEmailDto dto = new("updated@example.com");

        // Act
        Result result = await helper.ProfileService.ChangeEmailAsync(userId, dto);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        IdentityUser? user = await helper.GetUserByIdAsync(userId);
        user.ShouldNotBeNull();
        user.Email.ShouldBe("updated@example.com");
        user.UserName.ShouldBe("updated@example.com");
    }

    [Fact]
    public async Task ChangeEmailAsync_SameEmail_Succeeds()
    {
        // Arrange
        await using ProfileTestHelper helper = await ProfileTestHelper.CreateAsync();
        string userId = await helper.SeedUserAsync("same@example.com");

        ChangeEmailDto dto = new("same@example.com");

        // Act
        Result result = await helper.ProfileService.ChangeEmailAsync(userId, dto);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ChangeEmailAsync_EmailTaken_ReturnsFailure()
    {
        // Arrange
        await using ProfileTestHelper helper = await ProfileTestHelper.CreateAsync();
        await helper.SeedUserAsync("existing@example.com");
        string userId = await helper.SeedUserAsync("original@example.com");

        ChangeEmailDto dto = new("existing@example.com");

        // Act
        Result result = await helper.ProfileService.ChangeEmailAsync(userId, dto);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldBe("This email is already in use by another account.");
    }

    [Fact]
    public async Task ChangeEmailAsync_InvalidEmail_ReturnsFailure()
    {
        // Arrange
        await using ProfileTestHelper helper = await ProfileTestHelper.CreateAsync();
        string userId = await helper.SeedUserAsync("test@example.com");

        ChangeEmailDto dto = new("invalid-email");

        // Act
        Result result = await helper.ProfileService.ChangeEmailAsync(userId, dto);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldBe("Email must be a valid email address.");
    }

    [Fact]
    public async Task ChangeEmailAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        await using ProfileTestHelper helper = await ProfileTestHelper.CreateAsync();

        ChangeEmailDto dto = new("new@example.com");

        // Act
        Result result = await helper.ProfileService.ChangeEmailAsync("non-existent-id", dto);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldBe("User not found.");
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidPassword_UpdatesPassword()
    {
        // Arrange
        await using ProfileTestHelper helper = await ProfileTestHelper.CreateAsync();
        string userId = await helper.SeedUserAsync("test@example.com", "oldpassword1");

        ChangePasswordDto dto = new("oldpassword1", "newpassword1", "newpassword1");

        // Act
        Result result = await helper.ProfileService.ChangePasswordAsync(userId, dto);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify new password works
        bool canLogin = await helper.CheckPasswordAsync(userId, "newpassword1");
        canLogin.ShouldBeTrue();

        // Verify old password doesn't work
        bool oldWorks = await helper.CheckPasswordAsync(userId, "oldpassword1");
        oldWorks.ShouldBeFalse();
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ReturnsFailure()
    {
        // Arrange
        await using ProfileTestHelper helper = await ProfileTestHelper.CreateAsync();
        string userId = await helper.SeedUserAsync("test@example.com", "correctpassword");

        ChangePasswordDto dto = new("wrongpassword", "newpassword1", "newpassword1");

        // Act
        Result result = await helper.ProfileService.ChangePasswordAsync(userId, dto);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldBe("Current password is incorrect.");
    }

    [Fact]
    public async Task ChangePasswordAsync_PasswordTooShort_ReturnsFailure()
    {
        // Arrange
        await using ProfileTestHelper helper = await ProfileTestHelper.CreateAsync();
        string userId = await helper.SeedUserAsync("test@example.com", "oldpassword1");

        ChangePasswordDto dto = new("oldpassword1", "short", "short");

        // Act
        Result result = await helper.ProfileService.ChangePasswordAsync(userId, dto);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldBe("Password must be at least 8 characters.");
    }

    [Fact]
    public async Task ChangePasswordAsync_PasswordsMismatch_ReturnsFailure()
    {
        // Arrange
        await using ProfileTestHelper helper = await ProfileTestHelper.CreateAsync();
        string userId = await helper.SeedUserAsync("test@example.com", "oldpassword1");

        ChangePasswordDto dto = new("oldpassword1", "newpassword1", "differentpassword");

        // Act
        Result result = await helper.ProfileService.ChangePasswordAsync(userId, dto);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldBe("Passwords do not match.");
    }

    [Fact]
    public async Task ChangePasswordAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        await using ProfileTestHelper helper = await ProfileTestHelper.CreateAsync();

        ChangePasswordDto dto = new("oldpassword1", "newpassword1", "newpassword1");

        // Act
        Result result = await helper.ProfileService.ChangePasswordAsync("non-existent-id", dto);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldBe("User not found.");
    }
}
