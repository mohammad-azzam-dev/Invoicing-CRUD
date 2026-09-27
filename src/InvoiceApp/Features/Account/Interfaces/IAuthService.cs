using InvoiceApp.Features.Account.Dtos;
using Microsoft.AspNetCore.Identity;

namespace InvoiceApp.Features.Account.Interfaces;

public interface IAuthService
{
    Task<SignInResult> LoginAsync(LoginDto dto);
    Task LogoutAsync();
}
