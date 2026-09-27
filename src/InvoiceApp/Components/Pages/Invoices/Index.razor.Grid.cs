using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices;
using InvoiceApp.Features.Invoices.Dtos;
using Radzen;

namespace InvoiceApp.Components.Pages.Invoices;

public sealed partial class Index
{
    private async Task LoadDataAsync(LoadDataArgs args)
    {
        _isLoading = true;
        try
        {
            InvoiceQuery query = new InvoiceQuery(
                Search: _searchText,
                Status: _selectedStatus,
                SortBy: MapSortField(args.OrderBy),
                Descending: IsDescending(args.OrderBy),
                Page: (args.Skip ?? 0) / (args.Top ?? DefaultPageSize) + DefaultPage,
                PageSize: args.Top ?? DefaultPageSize
            );
            PagedResult<InvoiceListItemDto> result = await InvoiceService.GetPagedAsync(query);
            _invoices = result.Items;
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

    private async Task OnStatusFilterChanged(InvoiceStatus? value)
    {
        _selectedStatus = value;
        await ReloadGridAsync();
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

        // Check if current page will be empty after deletion
        int remainingCount = _count - 1;
        int firstItemOnCurrentPage = (_currentPage - 1) * _currentPageSize;
        bool currentPageWillBeEmpty = remainingCount <= firstItemOnCurrentPage && _currentPage > 1;

        if (currentPageWillBeEmpty)
        {
            await _grid.GoToPage(_currentPage - 2); // GoToPage is 0-indexed
        }
        else
        {
            await _grid.Reload();
        }
    }

    private static InvoiceSortField MapSortField(string? orderBy)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
        {
            return InvoiceSortField.IssueDate;
        }

        // Radzen sends "PropertyName asc" or "PropertyName desc"
        string field = orderBy.Split(' ')[0];

        return field.ToLowerInvariant() switch
        {
            "number" => InvoiceSortField.Number,
            "customername" => InvoiceSortField.CustomerName,
            "issuedate" => InvoiceSortField.IssueDate,
            "duedate" => InvoiceSortField.DueDate,
            "status" => InvoiceSortField.Status,
            "itemcount" => InvoiceSortField.ItemCount,
            "total" => InvoiceSortField.Total,
            _ => InvoiceSortField.IssueDate,
        };
    }

    private static bool IsDescending(string? orderBy)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
        {
            return true; // Default to descending (newest first)
        }

        return orderBy.Contains("desc", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetStatusBadgeStyle(InvoiceStatus status) =>
        status switch
        {
            InvoiceStatus.Draft => "background: var(--rz-secondary); color: white;",
            InvoiceStatus.Sent => "background: var(--rz-info); color: white;",
            InvoiceStatus.Paid => "background: var(--rz-success); color: white;",
            InvoiceStatus.Cancelled => "background: var(--rz-danger); color: white;",
            _ => "",
        };
}
