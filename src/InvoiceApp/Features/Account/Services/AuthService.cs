using FluentValidation;
using Microsoft.AspNetCore.Identity;
using InvoiceApp.Features.Account.Dtos;
using InvoiceApp.Features.Account.Interfaces;

namespace InvoiceApp.Features.Account.Services;

public sealed class AuthService : IAuthService
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IValidator<LoginDto> _validator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        SignInManager<IdentityUser> signInManager,
        IValidator<LoginDto> validator,
        ILogger<AuthService> logger)
    {
        _signInManager = signInManager;
        _validator = validator;
        _logger = logger;
    }

    public async Task<SignInResult> LoginAsync(LoginDto dto)
    {
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Login validation failed: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            return SignInResult.Failed;
        }

        var result = await _signInManager.PasswordSignInAsync(dto.Email, dto.Password, dto.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in.", dto.Email);
        }
        else if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} account locked out.", dto.Email);
        }

        return result;
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
    }
}
