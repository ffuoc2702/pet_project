using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.UserSportProfile;

public sealed record UpdateUserSportProfileCommand(Guid ActorUserId, Guid ProfileId, string Level,
    IReadOnlyCollection<Guid> PreferredFormatIds, string? Bio) : IRequest<UserSportProfileDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.UpdateSportProfiles];
}

public sealed class UpdateUserSportProfileCommandHandler(
                        IUserSportProfileRepository profilesRepository,
                        IUnitOfWork unitOfWork, 
                        IMapper mapper) : IRequestHandler<UpdateUserSportProfileCommand, UserSportProfileDto>
{
    public async Task<UserSportProfileDto> Handle(UpdateUserSportProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await profilesRepository.GetForUpdateAsync(request.ProfileId, request.ActorUserId, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.UserSportProfileNotFound, "Không tìm thấy hồ sơ thể thao của bạn.");
        try
        {
            profile.UpdateDetails(request.Level, request.PreferredFormatIds, request.Bio);
        }
        catch (ArgumentException exception)
        {
            throw new RequestValidationException(ErrorCodes.InvalidUserSportProfile, "Thông tin hồ sơ thể thao không hợp lệ.",
                new Dictionary<string, string[]> { [exception.ParamName ?? "profile"] = [exception.GetValidationMessage()] });
        }
        profilesRepository.Update(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<UserSportProfileDto>(profile);
    }
}
