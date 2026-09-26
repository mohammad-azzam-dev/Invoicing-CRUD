using Radzen;

namespace InvoiceApp.Components.Shared;

public static class DialogServiceExtensions
{
    public static async Task<bool> ConfirmAsync(
        this DialogService dialogs,
        string message,
        string title = "Confirm",
        string okText = "Confirm",
        string cancelText = "Cancel")
    {
        bool? result = await dialogs.Confirm(message, title, new ConfirmOptions
        {
            OkButtonText = okText,
            CancelButtonText = cancelText
        });

        return result == true;
    }
}
