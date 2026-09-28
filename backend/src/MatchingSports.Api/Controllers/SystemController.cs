using MatchingSports.Api.Contracts.Common;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[Route("api/v1")]
public sealed class SystemController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<BaseResponse<ApiInfoResponse>>(StatusCodes.Status200OK)]
    public ActionResult<BaseResponse<ApiInfoResponse>> GetApiInfo() => OkResponse(
        new ApiInfoResponse("MatchingSports.Api", "v1", "PostgreSQL/PostGIS"));
}

public sealed record ApiInfoResponse(string Service, string Version, string Database);
