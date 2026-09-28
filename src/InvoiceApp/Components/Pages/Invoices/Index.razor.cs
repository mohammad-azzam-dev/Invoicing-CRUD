using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Interfaces;
using InvoiceApp.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Radzen;
using Radzen.Blazor;

namespace InvoiceApp.Components.Pages.Invoices;

public sealed partial class Index : IDisposable
{
    [Inject]
    private IInvoiceQueryService QueryService { get; set; } = default!;

    [Inject]
    private IInvoiceCommandService CommandService { get; set; } = default!;

    [Inject]
    private DialogService DialogService { get; set; } = default!;

    [Inject]
    private NotificationService NotificationService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IOptions<PaginationSettings> PaginationOptions { get; set; } = default!;

    private PaginationSettings Pagination => PaginationOptions.Value;

    private IEnumerable<InvoiceListItemDto>? _invoices;
    private int _count;
    private bool _isLoading;

    private string? _searchText;
    private InvoiceStatus? _selectedStatus;
    private Timer? _debounceTimer;
    private int _currentPage;
    private int _currentPageSize;

    private RadzenDataGrid<InvoiceListItemDto>? _grid;

    private IReadOnlyList<int> PageSizeOptions => Pagination.PageSizeOptions;

    public void Dispose()
    {
        _debounceTimer?.Dispose();
    }
}
