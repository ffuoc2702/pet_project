using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.UserProfile;

public sealed record UpdateUserProfileCommand(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    string? DefaultArea,
    string? ContactNote) : IRequest<UserProfileDto>;

public sealed class UpdateUserProfileCommandHandler(
    IUserProfileRepository userProfileRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<UpdateUserProfileCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(
        UpdateUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await userProfileRepository.GetForUpdateAsync(
            request.UserId,
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
