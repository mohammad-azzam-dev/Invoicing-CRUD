using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Routing;

internal static class IdentityComponentsEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/Account");

        accountGroup.MapPost(
            "/Logout",
            async (
                ClaimsPrincipal user,
                [FromServices] SignInManager<IdentityUser> signInManager,
                [FromForm] string returnUrl
            ) =>
            {
                await signInManager.SignOutAsync();
                return TypedResults.LocalRedirect(returnUrl);
            }
        );

        return accountGroup;
    }
}
