using AutoMapper;
using MatchingSports.Application.Abstractions;
using MediatR;

namespace MatchingSports.Application.Features.UserPreferences;

public sealed record GetUserPreferencesQuery : IRequest<UserPreferencesDto?>;

public sealed class GetUserPreferencesQueryHandler(ICurrentUser currentUser,
    IUserPreferencesRepository repository, IMapper mapper)
    : IRequestHandler<GetUserPreferencesQuery, UserPreferencesDto?>
{
    public async Task<UserPreferencesDto?> Handle(GetUserPreferencesQuery request, CancellationToken cancellationToken)
    {
        var preferences = await repository.GetAsync(currentUser.UserId, false, cancellationToken);
        return preferences is null ? null : mapper.Map<UserPreferencesDto>(preferences);
    }
}
