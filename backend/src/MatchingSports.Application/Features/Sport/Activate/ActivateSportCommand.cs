using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Sport;

public sealed record ActivateSportCommand(Guid ActorUserId, Guid SportId)
    : IRequest<SportDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.UpdateSports];
}

public sealed class ActivateSportCommandHandler(ISportRepository sports, IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<ActivateSportCommand, SportDto>
{
    public async Task<SportDto> Handle(ActivateSportCommand request, CancellationToken cancellationToken)
    {
        var sport = await sports.GetForUpdateAsync(request.SportId, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.SportNotFound, "Không tìm thấy môn thể thao.");

        if (!sport.IsActive && await sports.HasActiveDuplicateAsync(sport.Code, sport.Name, sport.Id, cancellationToken))
            throw new ConflictException(ErrorCodes.DuplicateResource, "Mã hoặc tên đã được sử dụng bởi môn thể thao đang hoạt động.");

        try
        {
            sport.Activate();
        }
        catch (ArgumentException exception)
        {
            throw new RequestValidationException(ErrorCodes.InvalidSport, "Thông tin môn thể thao không hợp lệ.",
                new Dictionary<string, string[]> { [exception.ParamName ?? "sport"] = [exception.GetValidationMessage()] });
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<SportDto>(sport);
    }
}
