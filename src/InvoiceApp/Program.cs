using FluentValidation;
using InvoiceApp.Components;
using InvoiceApp.Components.Account;
using InvoiceApp.Data;
using InvoiceApp.Data.Repositories;
using InvoiceApp.Features.Account.Interfaces;
using InvoiceApp.Features.Account.Services;
using InvoiceApp.Features.Account.Validators;
using InvoiceApp.Features.Invoices.Interfaces;
using InvoiceApp.Features.Invoices.Services;
using InvoiceApp.Features.Invoices.Validators;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<
    AuthenticationStateProvider,
    IdentityRevalidatingAuthenticationStateProvider
>();

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

string connectionString =
    builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");

builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder
    .Services.AddIdentityCore<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddValidatorsFromAssemblyContaining<LoginDtoValidator>();

builder.Services.AddScoped<IRegisterService, RegisterService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<InvoiceFormValidator>();
builder.Services.AddScoped<IInvoiceQueryService, InvoiceQueryService>();
builder.Services.AddScoped<IInvoiceCommandService, InvoiceCommandService>();

WebApplication app = builder.Build();

// Apply migrations and seed data (skip in Testing environment)
if (!app.Environment.IsEnvironment("Testing"))
{
    // Drop and recreate database in Development for fresh start
    if (app.Environment.IsDevelopment())
    {
        // Clear all SQLite connection pools to release file locks
        SqliteConnection.ClearAllPools();

        string dbPath = Path.Combine(app.Environment.ContentRootPath, "invoices.db");
        string[] filesToDelete = new[] { dbPath, dbPath + "-wal", dbPath + "-shm" };
        bool deletedAny = false;

        foreach (string file in filesToDelete.Where(File.Exists))
        {
            // Retry a few times in case of transient locks
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    File.Delete(file);
                    deletedAny = true;
                    break;
                }
                catch (IOException) when (attempt < 3)
                {
                    Thread.Sleep(100 * attempt);
                }
                catch (IOException)
                {
                    Console.WriteLine(
                        $"[Dev] Could not delete {Path.GetFileName(file)} - file is locked."
                    );
                    Console.WriteLine(
                        "[Dev] Close DB Browser or other tools to enable fresh database on next restart."
                    );
                    break; // Skip this file and continue
                }
            }
        }

        if (deletedAny)
        {
            Console.WriteLine("[Dev] Database deleted. Fresh data will be seeded.");
        }
    }

    await using var scope = app.Services.CreateAsyncScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();

    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();

public partial class Program;
