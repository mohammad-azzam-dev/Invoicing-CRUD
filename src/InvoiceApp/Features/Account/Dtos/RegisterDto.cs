namespace InvoiceApp.Features.Account.Dtos;

public record RegisterDto(string Email, string Password, string ConfirmPassword);
