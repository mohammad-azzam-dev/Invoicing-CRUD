using System.ComponentModel.DataAnnotations;
using InvoiceApp.Components.Shared;
using InvoiceApp.Domain;
using InvoiceApp.Features.Invoices.Dtos;

namespace InvoiceApp.Components.Pages.Invoices.Forms;

public class LineItemFormModel : IFormModel<LineItemFormDto>
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [MaxLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
    public string Description { get; set; } = "";

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
    public decimal Quantity { get; set; } = 1;

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Unit price cannot be negative.")]
    public decimal UnitPrice { get; set; }

    [Required]
    [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100.")]
    public decimal DiscountPercent { get; set; }

    public decimal LineTotal =>
        InvoiceCalculations.CalculateLineTotal(Quantity, UnitPrice, DiscountPercent);

    public LineItemFormDto ToDto()
    {
        return new LineItemFormDto(Id, Description.Trim(), Quantity, UnitPrice, DiscountPercent);
    }

    public static LineItemFormModel FromDto(LineItemDto dto)
    {
        return new LineItemFormModel
        {
            Id = dto.Id,
            Description = dto.Description,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice,
            DiscountPercent = dto.DiscountPercent,
        };
    }

    public static LineItemFormModel CreateNew()
    {
        return new LineItemFormModel
        {
            Id = 0,
            Description = "",
            Quantity = 1,
            UnitPrice = 0,
            DiscountPercent = 0,
        };
    }
}
