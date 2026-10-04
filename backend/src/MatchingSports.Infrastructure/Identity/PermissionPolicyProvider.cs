using MatchingSports.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace MatchingSports.Infrastructure.Identity;

internal sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var configuredPolicy = await base.GetPolicyAsync(policyName);
        if (configuredPolicy is not null)
            return configuredPolicy;
        if (string.IsNullOrWhiteSpace(policyName) || policyName.Length > 100
            || policyName.Any(character => !char.IsAsciiLetterOrDigit(character)
                && character is not '.' and not '_' and not '-'))
            return null;

        return new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
            .RequireClaim(AuthorizationClaimTypes.Permission, policyName).Build();
    }
}
