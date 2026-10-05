using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;
using SportEntity = MatchingSports.Domain.Entities.Sport;

namespace MatchingSports.Application.Features.Sport;

public sealed record CreateSportCommand(Guid ActorUserId, string Code, string Name, IReadOnlyCollection<SportFormatInput> Formats)
    : IRequest<SportDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.CreateSports];
}

public sealed class CreateSportCommandHandler(ISportRepository sports, IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<CreateSportCommand, SportDto>
{
    public async Task<SportDto> Handle(CreateSportCommand request, CancellationToken cancellationToken)
    {
        SportEntity sport;
        try
        {
            var formats = SportFormatInput.ToDetails(request.Formats);
            sport = SportEntity.Create(request.Code, request.Name, formats);
        }
        catch (ArgumentException exception)
        {
            throw new RequestValidationException(ErrorCodes.InvalidSport, "Thông tin môn thể thao không hợp lệ.",
                new Dictionary<string, string[]> { [exception.ParamName ?? "sport"] = [exception.GetValidationMessage()] });
        }

        if (await sports.HasActiveDuplicateAsync(sport.Code, sport.Name, null, cancellationToken))
            throw new ConflictException(ErrorCodes.DuplicateResource, "Mã hoặc tên đã được sử dụng bởi môn thể thao đang hoạt động.");

        sports.Add(sport);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<SportDto>(sport);
    }
}
