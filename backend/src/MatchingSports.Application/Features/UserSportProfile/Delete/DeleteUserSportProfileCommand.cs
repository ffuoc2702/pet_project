using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.UserSportProfile;

public sealed record DeleteUserSportProfileCommand(Guid ActorUserId, Guid ProfileId) : IRequest, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.UpdateSportProfiles];
}

public sealed class DeleteUserSportProfileCommandHandler(ICurrentUser currentUser, IUserSportProfileRepository profiles,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteUserSportProfileCommand>
{
    public async Task Handle(DeleteUserSportProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (request.ActorUserId != userId)
            throw new ForbiddenException(ErrorCodes.Forbidden, "Bạn chỉ được thay đổi hồ sơ thể thao của mình.");
        var profile = await profiles.GetForUpdateAsync(request.ProfileId, userId, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.UserSportProfileNotFound, "Không tìm thấy hồ sơ thể thao của bạn.");
        profiles.Remove(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
