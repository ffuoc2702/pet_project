using MatchingSports.Application.Abstractions;
using MediatR;

namespace MatchingSports.Application.Common;

public sealed class AuditActorBehavior<TRequest, TResponse>(IAuditActorContext auditActorContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IPermissionAuthorizedRequest
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        using var scope = auditActorContext.BeginScope(request.ActorUserId);
        return await next(cancellationToken);
    }
}
