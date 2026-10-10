using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;
using PreferencesEntity = MatchingSports.Domain.Entities.UserPreferences;

namespace MatchingSports.Application.Features.UserPreferences;

public sealed record UpdateUserPreferencesCommand(string LanguageCode, string ColorMode,
    string ThemePreset, Guid? ConcurrencyToken) : IRequest<UserPreferencesDto>;

public sealed class UpdateUserPreferencesCommandHandler(ICurrentUser currentUser,
    IUserPreferencesRepository repository, IUnitOfWork unitOfWork, IAuditActorContext auditActorContext,
    IMapper mapper) : IRequestHandler<UpdateUserPreferencesCommand, UserPreferencesDto>
{
    public async Task<UserPreferencesDto> Handle(UpdateUserPreferencesCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        using var scope = auditActorContext.BeginScope(userId);
        var preferences = await repository.GetAsync(userId, true, cancellationToken);
        if (preferences is not null && preferences.ConcurrencyToken != request.ConcurrencyToken
            || preferences is null && request.ConcurrencyToken is not null)
            throw new ConflictException(ErrorCodes.ConcurrencyConflict, "Tùy chọn đã thay đổi; hãy tải lại.");
        var isNew = preferences is null;
        preferences ??= PreferencesEntity.Create(userId);
        try { preferences.Update(request.LanguageCode, request.ColorMode, request.ThemePreset); }
        catch (ArgumentException exception)
        {
            throw new RequestValidationException(ErrorCodes.InvalidUserPreferences, "Tùy chọn không hợp lệ.",
                new Dictionary<string, string[]> { [exception.ParamName ?? "preferences"] = [exception.GetValidationMessage()] });
        }
        if (isNew)
            repository.Add(preferences);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<UserPreferencesDto>(preferences);
    }
}
