using System.ComponentModel.DataAnnotations;
using InvoiceApp.Features.Account.Dtos;

namespace InvoiceApp.Components.Account.Forms;

public class LoginFormModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = "";

    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }

    public LoginDto ToDto() => new(Email, Password, RememberMe);
}
