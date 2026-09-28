using System.ComponentModel.DataAnnotations;
using InvoiceApp.Components.Shared;
using InvoiceApp.Domain;
using InvoiceApp.Features.Customers.Dtos;
using InvoiceApp.Features.Customers.Interfaces;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace InvoiceApp.Components.Pages.Customers;

public sealed partial class CustomerFormDialog
{
    [Inject]
    private ICustomerService CustomerService { get; set; } = default!;

    [Inject]
    private DialogService DialogService { get; set; } = default!;

    [Parameter]
    public int? CustomerId { get; set; }

    [Parameter]
    public EventCallback<bool> OnClose { get; set; }

    private CustomerFormModel _form = new();
    private CustomerFormModel _originalForm = new();
    private bool _isLoading;
    private bool _isSaving;
    private string? _errorMessage;

    private bool IsEditMode => CustomerId.HasValue && CustomerId.Value > 0;

    protected override async Task OnInitializedAsync()
    {
        if (IsEditMode)
        {
            _isLoading = true;
            try
            {
                CustomerDetailsDto? details = await CustomerService.GetDetailsAsync(CustomerId!.Value);
                if (details is not null)
                {
                    _form = new CustomerFormModel
                    {
                        Name = details.Name,
                        CompanyName = details.CompanyName ?? "",
                        Email = details.Email,
                        Phone = details.Phone,
                        Address = details.Address ?? "",
                    };
                    _originalForm = new CustomerFormModel
                    {
                        Name = details.Name,
                        CompanyName = details.CompanyName ?? "",
                        Email = details.Email,
                        Phone = details.Phone,
                        Address = details.Address ?? "",
                    };
                }
            }
            finally
            {
                _isLoading = false;
            }
        }
    }

    private async Task OnSubmitAsync()
    {
        _isSaving = true;
        _errorMessage = null;

        try
        {
            CustomerFormDto dto = _form.ToDto(CustomerId);

            if (IsEditMode)
            {
                Result result = await CustomerService.UpdateAsync(CustomerId!.Value, dto);
                if (!result.IsSuccess)
                {
                    _errorMessage = result.Error ?? "An unexpected error occurred.";
                    return;
                }
            }
            else
            {
                Result<int> result = await CustomerService.CreateAsync(dto);
                if (!result.IsSuccess)
                {
                    _errorMessage = result.Error ?? "An unexpected error occurred.";
                    return;
                }
            }

            await OnClose.InvokeAsync(true);
            DialogService.Close(true);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task OnCancelAsync()
    {
        if (HasChanges())
        {
            bool confirmed = await DialogService.ConfirmAsync(
                "You have unsaved changes. Are you sure you want to close?",
                "Unsaved Changes",
                "Close"
            );

            if (!confirmed)
            {
                return;
            }
        }

        await OnClose.InvokeAsync(false);
        DialogService.Close(false);
    }

    private bool HasChanges()
    {
        return _form.Name != _originalForm.Name
            || _form.CompanyName != _originalForm.CompanyName
            || _form.Email != _originalForm.Email
            || _form.Phone != _originalForm.Phone
            || _form.Address != _originalForm.Address;
    }

    private sealed class CustomerFormModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name must not exceed 100 characters.")]
        public string Name { get; set; } = "";

        [MaxLength(150, ErrorMessage = "Company name must not exceed 150 characters.")]
        public string CompanyName { get; set; } = "";

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
        [MaxLength(150, ErrorMessage = "Email must not exceed 150 characters.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Phone is required.")]
        [MaxLength(30, ErrorMessage = "Phone must not exceed 30 characters.")]
        public string Phone { get; set; } = "";

        [MaxLength(300, ErrorMessage = "Address must not exceed 300 characters.")]
        public string Address { get; set; } = "";

        public CustomerFormDto ToDto(int? id) =>
            new(
                Id: id,
                Name: Name.Trim(),
                Email: Email.Trim(),
                Phone: Phone.Trim(),
                CompanyName: string.IsNullOrWhiteSpace(CompanyName) ? null : CompanyName.Trim(),
                Address: string.IsNullOrWhiteSpace(Address) ? null : Address.Trim()
            );
    }
}
