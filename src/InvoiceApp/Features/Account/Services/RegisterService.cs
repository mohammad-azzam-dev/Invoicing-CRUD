using FluentValidation;
using Microsoft.AspNetCore.Identity;
using InvoiceApp.Features.Account.Dtos;
using InvoiceApp.Features.Account.Interfaces;

namespace InvoiceApp.Features.Account.Services;

public sealed class RegisterService(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    IValidator<RegisterDto> validator,
    ILogger<RegisterService> logger) : IRegisterService
{
    public async Task<IdentityResult> CreateUserAsync(RegisterDto dto, CancellationToken ct = default)
    {
        var validationFailure = await ValidateAsync(dto, ct);
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var user = new IdentityUser
        {
            UserName = dto.Email,
            Email = dto.Email
        };

        var result = await userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            logger.LogWarning("Failed to create user {Email}: {Errors}",
                dto.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            return result;
        }

        logger.LogInformation("User {Email} created a new account.", dto.Email);
        await signInManager.SignInAsync(user, isPersistent: false);

        return result;
    }

    private async Task<IdentityResult?> ValidateAsync(RegisterDto dto, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(dto, ct);
        if (result.IsValid)
        {
            return null;
        }

        logger.LogWarning("Registration validation failed: {Errors}",
            string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));

        var identityErrors = result.Errors
            .Select(e => new IdentityError { Code = e.PropertyName, Description = e.ErrorMessage });

        return IdentityResult.Failed(identityErrors.ToArray());
    }
}
