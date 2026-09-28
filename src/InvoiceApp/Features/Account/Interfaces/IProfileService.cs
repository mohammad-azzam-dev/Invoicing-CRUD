using InvoiceApp.Domain;
using InvoiceApp.Features.Account.Dtos;

namespace InvoiceApp.Features.Account.Interfaces;

public interface IProfileService
{
    Task<string?> GetEmailAsync(string userId, CancellationToken ct = default);

    Task<Result> ChangeEmailAsync(string userId, ChangeEmailDto dto, CancellationToken ct = default);

    Task<Result> ChangePasswordAsync(string userId, ChangePasswordDto dto, CancellationToken ct = default);
}
