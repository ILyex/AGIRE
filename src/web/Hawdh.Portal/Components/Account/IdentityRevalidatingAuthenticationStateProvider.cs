using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Hawdh.Portal.Data;

namespace Hawdh.Portal.Components.Account;

// Revalidate the connected user's active status, security stamp, and administrator MFA state regularly.
internal sealed class IdentityRevalidatingAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        IOptions<IdentityOptions> options,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(5);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        // Get the user manager from a new scope to ensure it fetches fresh data
        await using var scope = scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(authenticationState.User);
        if (user is not { IsActive: true }
            || !await ValidateSecurityStampAsync(userManager, user, authenticationState.User))
            return false;

        var isAdministrator = await userManager.IsInRoleAsync(user, "Administrator");
        var twoFactorEnabled = configuration.GetValue<bool>("Security:EnableTwoFactor");
        return !twoFactorEnabled || environment.IsDevelopment() || environment.IsEnvironment("Local")
            || !isAdministrator || user.TwoFactorEnabled == authenticationState.User.HasClaim("hawdh:mfa", "true");
    }

    private async Task<bool> ValidateSecurityStampAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, ClaimsPrincipal principal)
    {
        if (!userManager.SupportsUserSecurityStamp)
        {
            return true;
        }
        else
        {
            var principalStamp = principal.FindFirstValue(options.Value.ClaimsIdentity.SecurityStampClaimType);
            var userStamp = await userManager.GetSecurityStampAsync(user);
            return principalStamp == userStamp;
        }
    }
}
