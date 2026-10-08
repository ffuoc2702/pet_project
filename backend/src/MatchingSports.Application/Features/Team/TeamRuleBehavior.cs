using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MediatR;

namespace MatchingSports.Application.Features.Team;

public sealed class TeamRuleBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try { return await next(cancellationToken); }
        catch (TeamRuleException exception) { throw new ConflictException(exception.Code, exception.Message); }
    }
}
