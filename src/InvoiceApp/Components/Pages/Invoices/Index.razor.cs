using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;
using InvoiceApp.Features.Invoices.Interfaces;
using Microsoft.AspNetCore.Components;
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

    private IEnumerable<InvoiceListItemDto>? _invoices;
    private int _count;
    private bool _isLoading;

    private string? _searchText;
    private InvoiceStatus? _selectedStatus;
    private Timer? _debounceTimer;
    private int _currentPage = DefaultPage;
    private int _currentPageSize = DefaultPageSize;

    private RadzenDataGrid<InvoiceListItemDto>? _grid;

    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;
    private static readonly IReadOnlyList<int> PageSizeOptions = [DefaultPageSize, 20, 50];

    public void Dispose()
    {
        _debounceTimer?.Dispose();
    }
}
