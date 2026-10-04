using System.Security.Claims;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;

namespace MatchingSports.Api.Identity;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            var value = user?.FindFirstValue(ClaimTypes.NameIdentifier);

            return user?.Identity?.IsAuthenticated == true && Guid.TryParse(value, out var userId)
                ? userId
                : throw new UnauthorizedException(
                    ErrorCodes.InvalidAccessToken,
                    "Access token không hợp lệ.");
        }
    }
}
