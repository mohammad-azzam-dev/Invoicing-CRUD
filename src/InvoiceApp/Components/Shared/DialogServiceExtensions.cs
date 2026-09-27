using Radzen;

namespace InvoiceApp.Components.Shared;

public static class DialogServiceExtensions
{
    public static async Task<bool> ConfirmAsync(
        this DialogService dialogService,
        string message,
        string title,
        string confirmText = "OK"
    )
    {
        bool? result = await dialogService.Confirm(
            message,
            title,
            new ConfirmOptions { OkButtonText = confirmText, CancelButtonText = "Cancel" }
        );

        return result == true;
    }
}
