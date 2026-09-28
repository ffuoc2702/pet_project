using AutoMapper;
using MatchingSports.Api.Contracts.Common;
using MatchingSports.Api.ErrorHandling;
using MatchingSports.Api.Middleware;
using MatchingSports.Application.Common.Pagination;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected BaseResponse<T> CreateResponse<T>(T data, string? message = null) => new()
    {
        Data = data,
        Message = message,
        TraceId = ApiProblemDetailsFactory.GetTraceId(HttpContext),
        CorrelationId = CorrelationIdMiddleware.GetCorrelationId(HttpContext)
    };

    protected PaginatedResponse<TDestination> CreatePaginatedResponse<TSource, TDestination>(
        PaginatedResult<TSource> result,
        Func<TSource, TDestination> map,
        string? message = null) => new()
        {
            Data = result.Items.Select(map).ToArray(),
            Message = message,
            TraceId = ApiProblemDetailsFactory.GetTraceId(HttpContext),
            CorrelationId = CorrelationIdMiddleware.GetCorrelationId(HttpContext),
            Pagination = new PaginationMetadata
            {
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalItems = result.TotalItems,
                TotalPages = result.TotalPages,
                HasPreviousPage = result.HasPreviousPage,
                HasNextPage = result.HasNextPage
            }
        };

    protected ActionResult<BaseResponse<T>> OkResponse<T>(T data, string? message = null) =>
        Ok(CreateResponse(data, message));

    protected ActionResult<PaginatedResponse<TDestination>> OkPaginatedResponse<TSource, TDestination>(
        PaginatedResult<TSource> result,
        Func<TSource, TDestination> map,
        string? message = null) =>
        Ok(CreatePaginatedResponse(result, map, message));

    protected CreatedAtActionResult CreatedResponse<T>(
        string actionName,
        object? routeValues,
        T data,
        string? message = null) =>
        CreatedAtAction(actionName, routeValues, CreateResponse(data, message));
}
