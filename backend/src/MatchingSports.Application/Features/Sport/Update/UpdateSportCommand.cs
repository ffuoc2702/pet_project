using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Sport;

public sealed record UpdateSportCommand(Guid ActorUserId, Guid SportId, string Code, string Name, IReadOnlyCollection<SportFormatInput> Formats)
    : IRequest<SportDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.UpdateSports];
}

public sealed class UpdateSportCommandHandler(ISportRepository sports, IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<UpdateSportCommand, SportDto>
{
    public async Task<SportDto> Handle(UpdateSportCommand request, CancellationToken cancellationToken)
    {
        var sport = await sports.GetForUpdateAsync(request.SportId, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.SportNotFound, "Không tìm thấy môn thể thao.");

        try
        {
            var formats = SportFormatInput.ToDetails(request.Formats);
            if (sport.IsActive && !string.IsNullOrWhiteSpace(request.Code) && !string.IsNullOrWhiteSpace(request.Name)
                && await sports.HasActiveDuplicateAsync(request.Code.Trim().ToUpperInvariant(), request.Name.Trim(), sport.Id, cancellationToken))
                throw new ConflictException(ErrorCodes.DuplicateResource, "Mã hoặc tên đã được sử dụng bởi môn thể thao đang hoạt động.");
            sport.UpdateDetails(request.Code, request.Name, formats);
        }
        catch (ArgumentException exception)
        {
            throw new RequestValidationException(ErrorCodes.InvalidSport, "Thông tin môn thể thao không hợp lệ.",
                new Dictionary<string, string[]> { [exception.ParamName ?? "sport"] = [exception.GetValidationMessage()] });
        }

        sports.Update(sport);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<SportDto>(sport);
    }
}
