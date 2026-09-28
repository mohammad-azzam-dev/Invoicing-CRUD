using FluentValidation;
using FluentValidation.Results;
using InvoiceApp.Domain;
using InvoiceApp.Features.Account.Dtos;
using InvoiceApp.Features.Account.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace InvoiceApp.Features.Account.Services;

public sealed class ProfileService(
    UserManager<IdentityUser> userManager,
    IValidator<ChangeEmailDto> emailValidator,
    IValidator<ChangePasswordDto> passwordValidator,
    ILogger<ProfileService> logger
) : IProfileService
{
    public async Task<string?> GetEmailAsync(string userId, CancellationToken ct = default)
    {
        IdentityUser? user = await userManager.FindByIdAsync(userId);
        return user?.Email;
    }

    public async Task<Result> ChangeEmailAsync(
        string userId,
        ChangeEmailDto dto,
        CancellationToken ct = default
    )
    {
        ValidationResult validationResult = await emailValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            string error = validationResult.Errors.First().ErrorMessage;
            logger.LogWarning("Email validation failed for user {UserId}: {Error}", userId, error);
            return Result.Failure(error);
        }

        IdentityUser? user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            logger.LogWarning("User not found for email change: {UserId}", userId);
            return Result.Failure("User not found.");
        }

        string newEmail = dto.NewEmail.Trim();

        // Check if email is the same
        if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Success(); // No change needed
        }

        // Check if email is already taken
        IdentityUser? existingUser = await userManager.FindByEmailAsync(newEmail);
        if (existingUser is not null)
        {
            logger.LogWarning(
                "Email already taken during profile update for user {UserId}: {Email}",
                userId,
                newEmail
            );
            return Result.Failure("This email is already in use by another account.");
        }

        IdentityResult setEmailResult = await userManager.SetEmailAsync(user, newEmail);
        if (!setEmailResult.Succeeded)
        {
            string error = setEmailResult.Errors.First().Description;
            logger.LogError("Failed to set email for user {UserId}: {Error}", userId, error);
            return Result.Failure($"Failed to update email: {error}");
        }

        IdentityResult setUserNameResult = await userManager.SetUserNameAsync(user, newEmail);
        if (!setUserNameResult.Succeeded)
        {
            string error = setUserNameResult.Errors.First().Description;
            logger.LogError("Failed to set username for user {UserId}: {Error}", userId, error);
            return Result.Failure($"Failed to update username: {error}");
        }

        logger.LogInformation("Email changed for user {UserId}", userId);
        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(
        string userId,
        ChangePasswordDto dto,
        CancellationToken ct = default
    )
    {
        ValidationResult validationResult = await passwordValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            string error = validationResult.Errors.First().ErrorMessage;
            logger.LogWarning("Password validation failed for user {UserId}: {Error}", userId, error);
            return Result.Failure(error);
        }

        IdentityUser? user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            logger.LogWarning("User not found for password change: {UserId}", userId);
            return Result.Failure("User not found.");
        }

        IdentityResult result = await userManager.ChangePasswordAsync(
            user,
            dto.CurrentPassword,
            dto.NewPassword
        );

        if (!result.Succeeded)
        {
            string error = result.Errors.First().Description;
            logger.LogWarning("Password change failed for user {UserId}: {Error}", userId, error);

            // Make the error message more user-friendly for incorrect current password
            if (error.Contains("Incorrect password", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure("Current password is incorrect.");
            }

            return Result.Failure(error);
        }

        logger.LogInformation("Password changed for user {UserId}", userId);
        return Result.Success();
    }
}
