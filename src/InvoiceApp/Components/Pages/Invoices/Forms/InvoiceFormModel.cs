using System.ComponentModel.DataAnnotations;
using InvoiceApp.Components.Shared;
using InvoiceApp.Features.Invoices.Dtos;

namespace InvoiceApp.Components.Pages.Invoices.Forms;

public class InvoiceFormModel : IFormModel<InvoiceFormDto>
{
    [Required(ErrorMessage = "Customer is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Customer is required.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Issue date is required.")]
    public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Due date is required.")]
    public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(30));

    [Required]
    [Range(0, 100, ErrorMessage = "Tax rate must be between 0 and 100.")]
    public decimal TaxRate { get; set; }

    public InvoiceFormDto ToDto()
    {
        return new InvoiceFormDto(CustomerId, IssueDate, DueDate, TaxRate);
    }

    public static InvoiceFormModel FromDto(InvoiceFormDto dto)
    {
        return new InvoiceFormModel
        {
            CustomerId = dto.CustomerId,
            IssueDate = dto.IssueDate,
            DueDate = dto.DueDate,
            TaxRate = dto.TaxRate,
        };
    }

    public static InvoiceFormModel FromDetails(InvoiceDetailsDto details)
    {
        return new InvoiceFormModel
        {
            CustomerId = details.CustomerId,
            IssueDate = details.IssueDate,
            DueDate = details.DueDate,
            TaxRate = details.TaxRate,
        };
    }
}
