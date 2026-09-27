using System.Net;
using InvoiceApp.Data;
using InvoiceApp.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace InvoiceApp.Tests.Feature;

public sealed class AuthenticationTests : IClassFixture<InvoiceAppFactory>
{
    private readonly InvoiceAppFactory _factory;

    public AuthenticationTests(InvoiceAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnonymousRequest_ToHomePage_RedirectsToLogin()
    {
        // Arrange
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().ShouldContain("/Account/Login");
    }

    [Fact]
    public async Task AnonymousRequest_ToInvoicesPage_RedirectsToLogin()
    {
        // Arrange
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        // Act
        var response = await client.GetAsync("/invoices");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().ShouldContain("/Account/Login");
    }

    [Fact]
    public async Task LoginPage_LoadsSuccessfully()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Account/Login");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("Log in");
        content.ShouldContain("Email");
        content.ShouldContain("Password");
    }

    [Fact]
    public async Task RegisterPage_LoadsSuccessfully()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Account/Register");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("Register");
        content.ShouldContain("Email");
        content.ShouldContain("Password");
    }

    [Fact]
    public async Task DbSeeder_CreatesDemoUser()
    {
        // Arrange
        await using var scope = _factory.Services.CreateAsyncScope();

        // Act
        await DbSeeder.SeedAsync(scope.ServiceProvider);

        // Assert
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(DbSeeder.DemoEmail);

        user.ShouldNotBeNull();
        user.Email.ShouldBe(DbSeeder.DemoEmail);
        user.EmailConfirmed.ShouldBeTrue();
    }

    [Fact]
    public async Task DbSeeder_IsIdempotent()
    {
        // Arrange
        await using var scope = _factory.Services.CreateAsyncScope();

        // Act - call twice
        await DbSeeder.SeedAsync(scope.ServiceProvider);
        await DbSeeder.SeedAsync(scope.ServiceProvider);

        // Assert - should still have exactly one user with that email
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(DbSeeder.DemoEmail);

        user.ShouldNotBeNull();
    }

    [Fact]
    public async Task DemoUser_PasswordIsValid()
    {
        // Arrange
        await using var scope = _factory.Services.CreateAsyncScope();
        await DbSeeder.SeedAsync(scope.ServiceProvider);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(DbSeeder.DemoEmail);

        // Act
        var passwordValid = await userManager.CheckPasswordAsync(user!, DbSeeder.DemoPassword);

        // Assert
        passwordValid.ShouldBeTrue();
    }
}
