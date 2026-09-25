using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InvoiceApp.Data;
using InvoiceApp.Features.Account.Dtos;
using InvoiceApp.Features.Account.Interfaces;

namespace InvoiceApp.Features.Account.Services;

public sealed class RegisterService(
    IDbContextFactory<AppDbContext> dbFactory,
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    IValidator<RegisterDto> validator,
    ILogger<RegisterService> logger) : IRegisterService
{
    public async Task<IdentityResult> CreateUserAsync(RegisterDto dto, CancellationToken ct = default)
    {
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("Registration validation failed: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var identityErrors = validationResult.Errors
                .Select(e => new IdentityError { Code = e.PropertyName, Description = e.ErrorMessage });
            return IdentityResult.Failed(identityErrors.ToArray());
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        try
        {
            var user = new IdentityUser
            {
                UserName = dto.Email,
                Email = dto.Email
            };

            var result = await userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                await transaction.RollbackAsync(ct);
                return result;
            }

            await transaction.CommitAsync(ct);

            logger.LogInformation("User {Email} created a new account.", dto.Email);
            await signInManager.SignInAsync(user, isPersistent: false);

            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            logger.LogError(ex, "Failed to create user {Email}", dto.Email);

            return IdentityResult.Failed(new IdentityError
            {
                Code = "CreateUserFailed",
                Description = "An error occurred while creating the user."
            });
        }
    }
}
