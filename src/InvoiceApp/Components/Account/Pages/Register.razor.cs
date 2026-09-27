using InvoiceApp.Components.Account;
using InvoiceApp.Components.Account.Forms;
using InvoiceApp.Features.Account.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;

namespace InvoiceApp.Components.Account.Pages;

public partial class Register
{
    private IEnumerable<IdentityError>? identityErrors;

    [Inject]
    private IRegisterService RegisterService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IdentityRedirectManager RedirectManager { get; set; } = default!;

    [SupplyParameterFromForm]
    private RegisterFormModel Input { get; set; } = default!;

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    private string? Message =>
        identityErrors is null
            ? null
            : $"Error: {string.Join(", ", identityErrors.Select(error => error.Description))}";

    protected override void OnInitialized()
    {
        Input ??= new();
    }

    private async Task RegisterUser(EditContext editContext)
    {
        var result = await RegisterService.CreateUserAsync(Input.ToDto());

        if (!result.Succeeded)
        {
            identityErrors = result.Errors;
            return;
        }

        RedirectManager.RedirectTo(ReturnUrl);
    }
}
