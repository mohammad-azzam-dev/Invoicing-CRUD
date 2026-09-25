namespace InvoiceApp.Features.Account.Dtos;

public record LoginDto(string Email, string Password, bool RememberMe);
