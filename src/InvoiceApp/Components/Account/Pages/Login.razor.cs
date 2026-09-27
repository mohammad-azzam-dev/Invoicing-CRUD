using InvoiceApp.Components.Account;
using InvoiceApp.Components.Account.Forms;
using InvoiceApp.Features.Account.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;

namespace InvoiceApp.Components.Account.Pages;

public partial class Login
{
    private string? errorMessage;
    private EditContext editContext = default!;

    [Inject]
    private IAuthService AuthService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IdentityRedirectManager RedirectManager { get; set; } = default!;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private LoginFormModel Input { get; set; } = default!;

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Input ??= new();
        editContext = new EditContext(Input);

        if (HttpMethods.IsGet(HttpContext.Request.Method))
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }

    private async Task LoginUser()
    {
        var result = await AuthService.LoginAsync(this.Input.ToDto());

        if (result.Succeeded)
        {
            RedirectManager.RedirectTo(ReturnUrl);
            return;
        }
        if (result.IsLockedOut)
        {
            errorMessage = "Account locked out.";
            return;
        }
        errorMessage = "Invalid login attempt.";
        return;
    }
}
