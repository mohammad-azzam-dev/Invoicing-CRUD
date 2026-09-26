using FluentValidation;
using Microsoft.AspNetCore.Identity;
using InvoiceApp.Features.Account.Dtos;
using InvoiceApp.Features.Account.Interfaces;

namespace InvoiceApp.Features.Account.Services;

public sealed class AuthService(
    SignInManager<IdentityUser> signInManager,
    IValidator<LoginDto> validator,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<SignInResult> LoginAsync(LoginDto dto)
    {
        var validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("Login validation failed: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            return SignInResult.Failed;
        }

        var result = await signInManager.PasswordSignInAsync(dto.Email, dto.Password, dto.RememberMe, lockoutOnFailure: false);

        if (result.IsLockedOut)
        {
            logger.LogWarning("User {Email} account locked out.", dto.Email);
            return result;
        }

        if (result.Succeeded)
        {
            logger.LogInformation("User {Email} logged in.", dto.Email);
        }

        return result;
    }

    public async Task LogoutAsync()
    {
        await signInManager.SignOutAsync();
        logger.LogInformation("User logged out.");
    }
}
