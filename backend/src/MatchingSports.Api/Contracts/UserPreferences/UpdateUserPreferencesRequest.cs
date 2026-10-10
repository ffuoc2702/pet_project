using System.ComponentModel.DataAnnotations;

namespace MatchingSports.Api.Contracts;

public sealed class UpdateUserPreferencesRequest
{
    [Required(ErrorMessage = "Ngôn ngữ là bắt buộc."), RegularExpression("^(vi|en)$", ErrorMessage = "Ngôn ngữ không được hỗ trợ.")]
    public string LanguageCode { get; init; } = "vi";
    [Required(ErrorMessage = "Chế độ nền là bắt buộc."), RegularExpression("^(light|dark|system)$", ErrorMessage = "Chế độ nền không được hỗ trợ.")]
    public string ColorMode { get; init; } = "system";
    [Required(ErrorMessage = "Bộ màu là bắt buộc."), RegularExpression("^(blue|green|purple)$", ErrorMessage = "Bộ màu không được hỗ trợ.")]
    public string ThemePreset { get; init; } = "blue";
    public Guid? ConcurrencyToken { get; init; }
}
