using System.ComponentModel.DataAnnotations;
using InvoiceApp.Components.Shared;
using InvoiceApp.Features.Account.Dtos;

namespace InvoiceApp.Components.Account.Forms;

public class RegisterFormModel : IFormModel<RegisterDto>
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, ErrorMessage = "Password must be at least {2} characters long.", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one number, and one special character.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirm password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";

    public RegisterDto ToDto()
    {
        return new RegisterDto(Email, Password, ConfirmPassword);
    }
}
