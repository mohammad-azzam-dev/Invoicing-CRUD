using System.ComponentModel.DataAnnotations;
using InvoiceApp.Components.Shared;
using InvoiceApp.Features.Account.Dtos;

namespace InvoiceApp.Components.Account.Forms;

public class LoginFormModel : IFormModel<LoginDto>
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }

    public LoginDto ToDto()
    {
        return new LoginDto(Email, Password, RememberMe);
    }
}
