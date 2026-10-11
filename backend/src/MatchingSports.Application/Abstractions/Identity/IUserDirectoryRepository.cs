using MatchingSports.Application.Common;
using MatchingSports.Application.Features.User;

namespace MatchingSports.Application.Abstractions;

public interface IUserDirectoryRepository
{
    Task<PaginatedResult<UserListDto>> ListAsync(GetUsersQuery query, CancellationToken cancellationToken);
}
