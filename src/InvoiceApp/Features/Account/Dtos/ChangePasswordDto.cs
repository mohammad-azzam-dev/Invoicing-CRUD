namespace InvoiceApp.Features.Account.Dtos;

public record ChangePasswordDto(string CurrentPassword, string NewPassword, string ConfirmPassword);
