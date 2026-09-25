using Microsoft.AspNetCore.Identity;

namespace InvoiceApp.Data;

public static class DbSeeder
{
    public const string DemoEmail = "admin@test.com";
    public const string DemoPassword = "12345678";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        var existingUser = await userManager.FindByEmailAsync(DemoEmail);
        if (existingUser is not null)
        {
            return;
        }

        var user = new IdentityUser
        {
            UserName = DemoEmail,
            Email = DemoEmail,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, DemoPassword);
    }
}
