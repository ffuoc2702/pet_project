using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.UserProfile;

public sealed record UpdateUserProfileCommand(
    string DisplayName,
    string? AvatarUrl,
    string? DefaultArea,
    string? ContactNote) : IRequest<UserProfileDto>;

public sealed class UpdateUserProfileCommandHandler(
    ICurrentUser currentUser,
    IUserProfileRepository userProfileRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<UpdateUserProfileCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(
        UpdateUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await userProfileRepository.GetForUpdateAsync(
            currentUser.UserId,
            cancellationToken);

        if (profile is null)
        {
            throw new NotFoundException(
                ErrorCodes.UserProfileNotFound,
                "Không tìm thấy hồ sơ người dùng.");
        }

        profile.UpdateDetails(
            request.DisplayName,
            request.AvatarUrl,
            request.DefaultArea,
            request.ContactNote);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<UserProfileDto>(profile);
    }
}
