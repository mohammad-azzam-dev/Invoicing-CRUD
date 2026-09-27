using InvoiceApp.Components.Pages.Invoices.Forms;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Radzen;

namespace InvoiceApp.Components.Pages.Invoices;

public partial class InvoiceForm
{
    [Parameter]
    public int Id { get; set; }

    [Inject]
    private IInvoiceService InvoiceService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private NotificationService NotificationService { get; set; } = null!;

    [Inject]
    private DialogService DialogService { get; set; } = null!;

    private InvoiceDetailsDto? _invoice;
    private InvoiceFormModel _formModel = new();
    private List<LineItemFormModel> _lineItems = [];
    private bool _isLoading = true;
    private bool _isSaving;
    private bool _hasUnsavedChanges;
    private bool _isReadOnly;

    private bool _isCreateMode => Id == 0;
    private string PageTitle => _isCreateMode ? "New Invoice" : (_invoice?.Number ?? "Invoice");

    protected override async Task OnInitializedAsync()
    {
        if (!_isCreateMode)
        {
            _invoice = await InvoiceService.GetDetailsAsync(Id);

            if (_invoice is not null)
            {
                _formModel = InvoiceFormModel.FromDetails(_invoice);
                _lineItems = _invoice.LineItems.Select(LineItemFormModel.FromDto).ToList();
                _isReadOnly = !_invoice.CanEdit;
            }
        }

        _isLoading = false;
    }

    private void OnFormChanged()
    {
        _hasUnsavedChanges = true;
    }

    private async Task SaveAsync(bool navigateAfterSave)
    {
        _isSaving = true;

        try
        {
            InvoiceFormDto formDto = _formModel.ToDto();
            List<LineItemFormDto> lineItemDtos = _lineItems.Select(i => i.ToDto()).ToList();

            var result = await InvoiceService.SaveAsync(Id, formDto, lineItemDtos);

            if (result.IsSuccess)
            {
                _hasUnsavedChanges = false;
                NotificationService.Notify(
                    NotificationSeverity.Success,
                    "Success",
                    _isCreateMode ? "Invoice created successfully." : "Invoice updated successfully."
                );

                if (navigateAfterSave)
                {
                    Navigation.NavigateTo("/invoices");
                    return;
                }

                if (_isCreateMode)
                {
                    // Navigate to edit page of newly created invoice
                    Navigation.NavigateTo($"/invoices/{result.Value}");
                    return;
                }

                // Refresh data after update
                _invoice = await InvoiceService.GetDetailsAsync(Id);
                if (_invoice is not null)
                {
                    _formModel = InvoiceFormModel.FromDetails(_invoice);
                    _lineItems = _invoice.LineItems.Select(LineItemFormModel.FromDto).ToList();
                }
                return;
            }

            NotificationService.Notify(
                NotificationSeverity.Error,
                "Error",
                result.Error ?? "Failed to save invoice."
            );
        }
        finally
        {
            _isSaving = false;
        }
    }

    private void GoBack()
    {
        Navigation.NavigateTo("/invoices");
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        if (_hasUnsavedChanges)
        {
            bool? result = await DialogService.Confirm(
                "You have unsaved changes. Are you sure you want to leave?",
                "Unsaved Changes",
                new ConfirmOptions { OkButtonText = "Leave", CancelButtonText = "Stay" }
            );

            if (result != true)
            {
                context.PreventNavigation();
            }
        }
    }
}
