using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MediatR;

namespace MatchingSports.Application.Features.Matching;

public sealed class MatchingRuleBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try { return await next(cancellationToken); }
        catch (MatchingRuleException exception) { throw new ConflictException(exception.Code, exception.Message); }
    }
}
