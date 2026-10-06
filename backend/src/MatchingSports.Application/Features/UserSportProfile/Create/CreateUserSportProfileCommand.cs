using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;
using ProfileEntity = MatchingSports.Domain.Entities.UserSportProfile;

namespace MatchingSports.Application.Features.UserSportProfile;

public sealed record CreateUserSportProfileCommand(Guid ActorUserId, Guid SportId, string Level,
    IReadOnlyCollection<Guid> PreferredFormatIds, string? Bio) : IRequest<UserSportProfileDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.UpdateSportProfiles];
}

public sealed class CreateUserSportProfileCommandHandler(ICurrentUser currentUser, IUserSportProfileRepository profiles,
    IUnitOfWork unitOfWork, IMapper mapper) : IRequestHandler<CreateUserSportProfileCommand, UserSportProfileDto>
{
    public async Task<UserSportProfileDto> Handle(CreateUserSportProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (request.ActorUserId != userId)
            throw new ForbiddenException(ErrorCodes.Forbidden, "Bạn chỉ được thay đổi hồ sơ thể thao của mình.");
        var sport = await profiles.GetSportAsync(request.SportId, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.SportNotFound, "Không tìm thấy môn thể thao.");
        ProfileEntity profile;
        try
        {
            profile = ProfileEntity.Create(userId, sport, request.Level, request.PreferredFormatIds, request.Bio);
        }
        catch (ArgumentException exception)
        {
            throw new RequestValidationException(ErrorCodes.InvalidUserSportProfile, "Thông tin hồ sơ thể thao không hợp lệ.",
                new Dictionary<string, string[]> { [exception.ParamName ?? "profile"] = [exception.GetValidationMessage()] });
        }
        if (await profiles.ExistsAsync(userId, request.SportId, cancellationToken))
            throw new ConflictException(ErrorCodes.DuplicateResource, "Bạn đã có hồ sơ cho môn thể thao này.");
        profiles.Add(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<UserSportProfileDto>(profile);
    }
}
