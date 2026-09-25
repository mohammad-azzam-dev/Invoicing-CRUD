using Microsoft.AspNetCore.Identity;
using InvoiceApp.Features.Account.Dtos;

namespace InvoiceApp.Features.Account.Interfaces;

public interface IAuthService
{
    Task<SignInResult> LoginAsync(LoginDto dto);
    Task LogoutAsync();
}
