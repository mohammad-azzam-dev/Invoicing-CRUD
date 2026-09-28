using InvoiceApp.Components.Shared;
using InvoiceApp.Components.Shared.Datatable;
using InvoiceApp.Domain;
using InvoiceApp.Features.Customers;
using InvoiceApp.Features.Customers.Dtos;
using InvoiceApp.Features.Customers.Interfaces;
using InvoiceApp.Features.Invoices;
using InvoiceApp.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Radzen;
using Radzen.Blazor;

namespace InvoiceApp.Components.Pages.Customers;

public sealed partial class Index : IDisposable
{
    [Inject]
    private ICustomerService CustomerService { get; set; } = default!;

    [Inject]
    private DialogService DialogService { get; set; } = default!;

    [Inject]
    private NotificationService NotificationService { get; set; } = default!;

    [Inject]
    private IOptions<PaginationSettings> PaginationOptions { get; set; } = default!;

    private PaginationSettings Pagination => PaginationOptions.Value;

    private IEnumerable<CustomerListItemDto>? _customers;
    private int _count;
    private bool _isLoading;

    private string? _searchText;
    private Timer? _debounceTimer;
    private int _currentPage;
    private int _currentPageSize;

    private RadzenDataGrid<CustomerListItemDto>? _grid;

    private IReadOnlyList<int> PageSizeOptions => Pagination.PageSizeOptions;

    private const string ActionEdit = "edit";
    private const string ActionDelete = "delete";

    public void Dispose()
    {
        _debounceTimer?.Dispose();
    }

    private async Task LoadDataAsync(LoadDataArgs args)
    {
        _isLoading = true;
        try
        {
            CustomerQuery query = new(
                Search: _searchText,
                SortBy: MapSortField(args.OrderBy),
                Descending: IsDescending(args.OrderBy),
                Page: (args.Skip ?? 0) / (args.Top ?? Pagination.DefaultPageSize) + Pagination.DefaultPage,
                PageSize: args.Top ?? Pagination.DefaultPageSize
            );
            PagedResult<CustomerListItemDto> result = await CustomerService.GetPagedAsync(query);
            _customers = result.Items;
            _count = result.TotalCount;
            _currentPage = query.Page;
            _currentPageSize = query.PageSize;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void OnSearchChanged(string? value)
    {
        _searchText = value;
        DebounceReload();
    }

    private void DebounceReload()
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(
            async _ =>
            {
                await InvokeAsync(async () =>
                {
                    await ReloadGridAsync();
                    StateHasChanged();
                });
            },
            null,
            300,
            Timeout.Infinite
        );
    }

    private async Task ReloadGridAsync()
    {
        if (_grid is not null)
        {
            await _grid.FirstPage(true);
        }
    }

    private async Task ReloadAfterDeleteAsync()
    {
        if (_grid is null)
        {
            return;
        }

        int remainingCount = _count - 1;
        int firstItemOnCurrentPage = (_currentPage - 1) * _currentPageSize;
        bool currentPageWillBeEmpty = remainingCount <= firstItemOnCurrentPage && _currentPage > 1;

        if (currentPageWillBeEmpty)
        {
            await _grid.GoToPage(_currentPage - 2);
        }
        else
        {
            await _grid.Reload();
        }
    }

    private static CustomerSortField MapSortField(string? orderBy)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
        {
            return CustomerSortField.DisplayName;
        }

        string field = orderBy.Split(' ')[0];

        return field.ToLowerInvariant() switch
        {
            "displayname" => CustomerSortField.DisplayName,
            "name" => CustomerSortField.Name,
            "email" => CustomerSortField.Email,
            "phone" => CustomerSortField.Phone,
            "invoicecount" => CustomerSortField.InvoiceCount,
            _ => CustomerSortField.DisplayName,
        };
    }

    private static bool IsDescending(string? orderBy)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
        {
            return false;
        }

        return orderBy.Contains("desc", StringComparison.OrdinalIgnoreCase);
    }

    private static List<ActionMenuItem> GetActionsForCustomer(CustomerListItemDto customer)
    {
        return
        [
            new ActionMenuItem("Edit", "edit", ActionEdit),
            new ActionMenuItem("Delete", "delete", ActionDelete),
        ];
    }

    private async Task OnActionSelected(CustomerListItemDto customer, string action)
    {
        switch (action)
        {
            case ActionEdit:
                await OpenEditDialogAsync(customer);
                break;
            case ActionDelete:
                await DeleteCustomerAsync(customer);
                break;
        }
    }

    private async Task OpenCreateDialogAsync()
    {
        bool? result = await DialogService.OpenAsync<CustomerFormDialog>(
            "New Customer",
            new Dictionary<string, object?>
            {
                { nameof(CustomerFormDialog.CustomerId), null },
                {
                    nameof(CustomerFormDialog.OnClose),
                    EventCallback.Factory.Create<bool>(
                        this,
                        saved =>
                        {
                            DialogService.Close(saved);
                        }
                    )
                },
            },
            new DialogOptions { Width = "500px", CloseDialogOnOverlayClick = false }
        );

        if (result == true)
        {
            NotificationService.Notify(
                new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Customer Created",
                    Detail = "The customer has been created successfully.",
                    Duration = 4000,
                }
            );

            await ReloadGridAsync();
        }
    }

    private async Task OpenEditDialogAsync(CustomerListItemDto customer)
    {
        bool? result = await DialogService.OpenAsync<CustomerFormDialog>(
            "Edit Customer",
            new Dictionary<string, object?>
            {
                { nameof(CustomerFormDialog.CustomerId), customer.Id },
                {
                    nameof(CustomerFormDialog.OnClose),
                    EventCallback.Factory.Create<bool>(
                        this,
                        saved =>
                        {
                            DialogService.Close(saved);
                        }
                    )
                },
            },
            new DialogOptions { Width = "500px", CloseDialogOnOverlayClick = false }
        );

        if (result == true)
        {
            NotificationService.Notify(
                new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Customer Updated",
                    Detail = $"Customer {customer.DisplayName} has been updated.",
                    Duration = 4000,
                }
            );

            if (_grid is not null)
            {
                await _grid.Reload();
            }
        }
    }

    private async Task DeleteCustomerAsync(CustomerListItemDto customer)
    {
        bool confirmed = await DialogService.ConfirmAsync(
            $"Delete {customer.DisplayName}? This action cannot be undone.",
            "Delete Customer",
            "Delete"
        );

        if (!confirmed)
        {
            return;
        }

        Result result = await CustomerService.DeleteAsync(customer.Id);

        if (result.IsSuccess)
        {
            NotificationService.Notify(
                new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Customer Deleted",
                    Detail = $"Customer {customer.DisplayName} has been deleted.",
                    Duration = 4000,
                }
            );

            await ReloadAfterDeleteAsync();
            return;
        }
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
