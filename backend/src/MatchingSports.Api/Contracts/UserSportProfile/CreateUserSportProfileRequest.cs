using System.ComponentModel.DataAnnotations;

namespace MatchingSports.Api.Contracts;

public sealed class CreateUserSportProfileRequest
{
    [Required(ErrorMessage = "ID môn thể thao là bắt buộc.")]
    public Guid SportId { get; init; }

    [Required(ErrorMessage = "Trình độ là bắt buộc.")]
    [RegularExpression("^(BEGINNER|INTERMEDIATE|ADVANCED)$", ErrorMessage = "Trình độ phải là BEGINNER, INTERMEDIATE hoặc ADVANCED.")]
    public string Level { get; init; } = "BEGINNER";

    [Required(ErrorMessage = "Danh sách thể thức là bắt buộc.")]
    public List<Guid> PreferredFormatIds { get; init; } = [];

    public string? Bio { get; init; }
}
