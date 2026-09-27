using InvoiceApp.Components.Pages.Invoices.Forms;
using InvoiceApp.Features.Invoices.Dtos;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;

namespace InvoiceApp.Components.Pages.Invoices;

public partial class LineItemsGrid
{
    [Parameter, EditorRequired]
    public List<LineItemFormModel> Items { get; set; } = [];

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public EventCallback OnChange { get; set; }

    private RadzenDataGrid<LineItemFormModel>? _grid;
    private LineItemFormModel? _itemToInsert;
    private LineItemFormModel? _itemBeforeEdit;

    private async Task AddLineItem()
    {
        // Don't allow adding another row if one is already being inserted
        if (_itemToInsert is not null)
        {
            return;
        }

        _itemToInsert = LineItemFormModel.CreateNew();
        Items.Add(_itemToInsert);
        await _grid!.InsertRow(_itemToInsert);
    }

    private async Task EditRow(LineItemFormModel item)
    {
        // Cancel any pending insert before editing another row
        if (_itemToInsert is not null)
        {
            Items.Remove(_itemToInsert);
            _itemToInsert = null;
            await _grid!.Reload();
        }

        _itemBeforeEdit = new LineItemFormModel
        {
            Id = item.Id,
            Description = item.Description,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            DiscountPercent = item.DiscountPercent,
        };
        await _grid!.EditRow(item);
    }

    private async Task SaveRow(LineItemFormModel item)
    {
        await _grid!.UpdateRow(item);
    }

    private void CancelEdit(LineItemFormModel item)
    {
        if (_itemToInsert == item)
        {
            Items.Remove(item);
            _itemToInsert = null;
        }
        else if (_itemBeforeEdit is not null)
        {
            item.Description = _itemBeforeEdit.Description;
            item.Quantity = _itemBeforeEdit.Quantity;
            item.UnitPrice = _itemBeforeEdit.UnitPrice;
            item.DiscountPercent = _itemBeforeEdit.DiscountPercent;
            _itemBeforeEdit = null;
        }

        _grid!.CancelEditRow(item);
    }

    private async Task DeleteRow(LineItemFormModel item)
    {
        Items.Remove(item);
        await _grid!.Reload();
        await OnChange.InvokeAsync();
    }

    private async Task OnRowCreate(LineItemFormModel item)
    {
        _itemToInsert = null;
        await OnChange.InvokeAsync();
    }

    private async Task OnRowUpdate(LineItemFormModel item)
    {
        _itemBeforeEdit = null;
        await OnChange.InvokeAsync();
    }
}
