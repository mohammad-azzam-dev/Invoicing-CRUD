using InvoiceApp.Features.Account.Dtos;
using Microsoft.AspNetCore.Identity;

namespace InvoiceApp.Features.Account.Interfaces;

public interface IRegisterService
{
    Task<IdentityResult> CreateUserAsync(RegisterDto dto, CancellationToken ct = default);
}
