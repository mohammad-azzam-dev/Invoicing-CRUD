using Microsoft.AspNetCore.Identity;
using InvoiceApp.Features.Account.Dtos;

namespace InvoiceApp.Features.Account.Interfaces;

public interface IRegisterService
{
    Task<IdentityResult> CreateUserAsync(RegisterDto dto, CancellationToken ct = default);
}
