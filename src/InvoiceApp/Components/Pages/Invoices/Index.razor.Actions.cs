using InvoiceApp.Components.Shared;
using InvoiceApp.Components.Shared.Datatable;
using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace InvoiceApp.Components.Pages.Invoices;

public sealed partial class Index
{
    private const string ActionView = "view";
    private const string ActionChangeStatus = "change-status";
    private const string ActionDelete = "delete";

    private Dictionary<string, Func<InvoiceListItemDto, Task>>? _actionHandlers;
    private Dictionary<string, Func<InvoiceListItemDto, Task>> ActionHandlers =>
        _actionHandlers ??= new()
        {
            [ActionView] = ViewInvoiceAsync,
            [ActionChangeStatus] = OpenChangeStatusDialogAsync,
            [ActionDelete] = DeleteInvoiceAsync,
        };

    private static List<ActionMenuItem> GetActionsForInvoice(InvoiceListItemDto invoice)
    {
        List<ActionMenuItem> actions = [];

        // View/Edit is always first
        string viewLabel = invoice.Status == InvoiceStatus.Draft ? "Edit" : "View";
        string viewIcon = invoice.Status == InvoiceStatus.Draft ? "edit" : "visibility";
        actions.Add(new ActionMenuItem(viewLabel, viewIcon, ActionView));

        if (invoice.AllowedNextStatuses.Any())
        {
            actions.Add(new ActionMenuItem("Change Status", "swap_horiz", ActionChangeStatus));
        }

        if (invoice.Status == InvoiceStatus.Draft)
        {
            actions.Add(new ActionMenuItem("Delete", "delete", ActionDelete));
        }

        return actions;
    }

    private async Task OnActionSelected(InvoiceListItemDto invoice, string action)
    {
        if (ActionHandlers.TryGetValue(action, out Func<InvoiceListItemDto, Task>? handler))
        {
            await handler(invoice);
        }
    }

    private Task ViewInvoiceAsync(InvoiceListItemDto invoice)
    {
        Navigation.NavigateTo($"/invoices/{invoice.Id}");
        return Task.CompletedTask;
    }

    private async Task DeleteInvoiceAsync(InvoiceListItemDto invoice)
    {
        bool confirmed = await DialogService.ConfirmAsync(
            $"Delete {invoice.Number}? This cannot be undone.",
            "Delete Invoice",
            "Delete"
        );

        if (!confirmed)
        {
            return;
        }

        Result result = await InvoiceService.DeleteAsync(invoice.Id);

        if (result.IsSuccess)
        {
            NotificationService.Notify(
                new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Invoice Deleted",
                    Detail = $"Invoice {invoice.Number} has been deleted.",
                    Duration = 4000,
                }
            );

            await ReloadAfterDeleteAsync();
        }
        else
        {
            NotificationService.Notify(
                new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Delete Failed",
                    Detail = result.Error ?? "An unexpected error occurred.",
                    Duration = 4000,
                }
            );
        }
    }

    private async Task OpenChangeStatusDialogAsync(InvoiceListItemDto invoice)
    {
        InvoiceStatus? selectedStatus = await DialogService.OpenAsync<ChangeStatusDialog>(
            "Change Status",
            new Dictionary<string, object?>
            {
                { nameof(ChangeStatusDialog.InvoiceNumber), invoice.Number },
                { nameof(ChangeStatusDialog.AllowedStatuses), invoice.AllowedNextStatuses },
                {
                    nameof(ChangeStatusDialog.OnClose),
                    EventCallback.Factory.Create<InvoiceStatus?>(
                        this,
                        status =>
                        {
                            DialogService.Close(status);
                        }
                    )
                },
            },
            new DialogOptions { Width = "300px", CloseDialogOnOverlayClick = true }
        );

        if (selectedStatus is null)
        {
            return;
        }

        if (selectedStatus == InvoiceStatus.Paid || selectedStatus == InvoiceStatus.Cancelled)
        {
            bool confirmed = await DialogService.ConfirmAsync(
                $"Mark {invoice.Number} as {selectedStatus}? This can't be undone.",
                "Confirm Status Change",
                "Confirm"
            );

            if (!confirmed)
            {
                return;
            }
        }

        Result result = await InvoiceService.ChangeStatusAsync(invoice.Id, selectedStatus.Value);

        if (!result.IsSuccess)
        {
            NotificationService.Notify(
                new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Status Change Failed",
                    Detail = result.Error ?? "An unexpected error occurred.",
                    Duration = 4000,
                }
            );
            return;
        }

        NotificationService.Notify(
            new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Status Changed",
                Detail = $"Invoice {invoice.Number} is now {selectedStatus}.",
                Duration = 4000,
            }
        );

        if (_grid is not null)
        {
            await _grid.Reload();
        }
    }
}
