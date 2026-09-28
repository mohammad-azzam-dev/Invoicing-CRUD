using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using InvoiceApp.Domain;
using InvoiceApp.Features.Account.Dtos;
using InvoiceApp.Features.Account.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;

namespace InvoiceApp.Components.Pages.Profile;

public sealed partial class Index
{
    [Inject]
    private IProfileService ProfileService { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    private NotificationService NotificationService { get; set; } = default!;

    private string? _userId;
    private string? _currentEmail;
    private bool _isLoading = true;

    private EmailFormModel _emailForm = new();
    private bool _isChangingEmail;
    private string? _emailError;

    private PasswordFormModel _passwordForm = new();
    private bool _isChangingPassword;
    private string? _passwordError;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _userId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (_userId is not null)
        {
            _currentEmail = await ProfileService.GetEmailAsync(_userId);
            _emailForm.NewEmail = _currentEmail ?? "";
        }

        _isLoading = false;
    }

    private async Task OnChangeEmailAsync()
    {
        if (_userId is null)
        {
            return;
        }

        _isChangingEmail = true;
        _emailError = null;

        try
        {
            ChangeEmailDto dto = new(_emailForm.NewEmail.Trim());
            Result result = await ProfileService.ChangeEmailAsync(_userId, dto);

            if (result.IsSuccess)
            {
                _currentEmail = _emailForm.NewEmail.Trim();

                NotificationService.Notify(
                    new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Email Updated",
                        Detail = "Your email has been updated successfully.",
                        Duration = 4000,
                    }
                );
            }
            else
            {
                _emailError = result.Error ?? "An unexpected error occurred.";
            }
        }
        finally
        {
            _isChangingEmail = false;
        }
    }

    private async Task OnChangePasswordAsync()
    {
        if (_userId is null)
        {
            return;
        }

        _isChangingPassword = true;
        _passwordError = null;

        try
        {
            ChangePasswordDto dto = new(
                _passwordForm.CurrentPassword,
                _passwordForm.NewPassword,
                _passwordForm.ConfirmPassword
            );
            Result result = await ProfileService.ChangePasswordAsync(_userId, dto);

            if (result.IsSuccess)
            {
                _passwordForm = new PasswordFormModel();

                NotificationService.Notify(
                    new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Password Updated",
                        Detail = "Your password has been updated successfully.",
                        Duration = 4000,
                    }
                );
            }
            else
            {
                _passwordError = result.Error ?? "An unexpected error occurred.";
            }
        }
        finally
        {
            _isChangingPassword = false;
        }
    }

    private sealed class EmailFormModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(256, ErrorMessage = "Email must not exceed 256 characters.")]
        public string NewEmail { get; set; } = "";
    }

    private sealed class PasswordFormModel
    {
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Please confirm your new password.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
