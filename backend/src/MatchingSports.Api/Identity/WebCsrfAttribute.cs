using MatchingSports.Api.ErrorHandling;
using MatchingSports.Shared;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MatchingSports.Api.Identity;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class WebCsrfAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        try
        {
            await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>()
                .ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new ObjectResult(ApiProblemDetailsFactory.Create(context.HttpContext,
                StatusCodes.Status403Forbidden, ErrorCodes.InvalidCsrfToken, "Yêu cầu không hợp lệ",
                "Vui lòng tải lại trang và thử lại."))
            { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
