using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.User;

public sealed record GetUsersQuery : BaseQuery<UserListDto>, IPermissionAuthorizedRequest
{
    public Guid ActorUserId { get; init; }
    public UserStatus? Status { get; init; }
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ListUsers];
}

public sealed class GetUsersQueryHandler(IUserDirectoryRepository users) : IRequestHandler<GetUsersQuery, PaginatedResult<UserListDto>>
{
    public Task<PaginatedResult<UserListDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        if (request.Status is { } status && !Enum.IsDefined(status))
            throw new RequestValidationException(ErrorCodes.ValidationFailed, "Trạng thái người dùng không hợp lệ.",
                new Dictionary<string, string[]> { ["status"] = ["Trạng thái người dùng không hợp lệ."] });
        return users.ListAsync(request, cancellationToken);
    }
}
