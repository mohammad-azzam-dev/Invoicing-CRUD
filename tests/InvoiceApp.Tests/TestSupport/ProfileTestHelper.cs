using FluentValidation;
using InvoiceApp.Data;
using InvoiceApp.Features.Account.Dtos;
using InvoiceApp.Features.Account.Interfaces;
using InvoiceApp.Features.Account.Services;
using InvoiceApp.Features.Account.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InvoiceApp.Tests.TestSupport;

/// <summary>
/// Helper for setting up profile service tests with real database.
/// </summary>
public sealed class ProfileTestHelper : IAsyncDisposable
{
    private readonly TestDatabase _testDb;
    private readonly AppDbContext _dbContext;

    public IDbContextFactory<AppDbContext> DbFactory => _testDb.Factory;
    public UserManager<IdentityUser> UserManager { get; }
    public IProfileService ProfileService { get; }

    private ProfileTestHelper(
        TestDatabase testDb,
        AppDbContext dbContext,
        UserManager<IdentityUser> userManager,
        IProfileService profileService
    )
    {
        _testDb = testDb;
        _dbContext = dbContext;
        UserManager = userManager;
        ProfileService = profileService;
    }

    public static async Task<ProfileTestHelper> CreateAsync()
    {
        TestDatabase testDb = await TestDatabase.CreateAsync();

        AppDbContext db = await testDb.Factory.CreateDbContextAsync();
        UserStore<IdentityUser> userStore = new(db);

        IdentityOptions identityOptions = new()
        {
            Password = new PasswordOptions
            {
                RequiredLength = 8,
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false,
                RequireNonAlphanumeric = false,
            },
        };
        IOptions<IdentityOptions> optionsAccessor = Options.Create(identityOptions);

        UserManager<IdentityUser> userManager = new(
            userStore,
            optionsAccessor,
            new PasswordHasher<IdentityUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<IdentityUser>>.Instance
        );

        IValidator<ChangeEmailDto> emailValidator = new ChangeEmailDtoValidator();
        IValidator<ChangePasswordDto> passwordValidator = new ChangePasswordDtoValidator();

        ProfileService profileService = new(
            userManager,
            emailValidator,
            passwordValidator,
            NullLogger<ProfileService>.Instance
        );

        return new ProfileTestHelper(testDb, db, userManager, profileService);
    }

    /// <summary>
    /// Seeds a user and returns their ID.
    /// </summary>
    public async Task<string> SeedUserAsync(
        string email = "test@example.com",
        string password = "password123"
    )
    {
        IdentityUser user = new()
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
        };

        IdentityResult result = await UserManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}"
            );
        }

        return user.Id;
    }

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    public async Task<IdentityUser?> GetUserByIdAsync(string userId)
    {
        return await UserManager.FindByIdAsync(userId);
    }

    /// <summary>
    /// Checks if password is valid for user.
    /// </summary>
    public async Task<bool> CheckPasswordAsync(string userId, string password)
    {
        IdentityUser? user = await UserManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }
        return await UserManager.CheckPasswordAsync(user, password);
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _testDb.DisposeAsync();
    }
}
