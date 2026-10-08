using System.ComponentModel.DataAnnotations;

namespace MatchingSports.Api.Contracts;

public sealed class SportFormatRequest : IValidatableObject
{
    public Guid? Id { get; init; }

    [Required(ErrorMessage = "Thao tác với thể thức là bắt buộc.")]
    [RegularExpression("^(NEW|MODIFY|DELETE)$", ErrorMessage = "Thao tác với thể thức phải là NEW, MODIFY hoặc DELETE.")]
    public string Action { get; init; } = null!;

    [StringLength(32, ErrorMessage = "Mã thể thức không được vượt quá 32 ký tự.")]
    public string? Code { get; init; }

    [StringLength(100, ErrorMessage = "Tên thể thức không được vượt quá 100 ký tự.")]
    public string? DisplayName { get; init; }

    public short SideSize { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Action == "NEW" && Id.HasValue)
            yield return new ValidationResult("Thể thức mới (NEW) không được có ID.", [nameof(Id)]);
        if (Action is "MODIFY" or "DELETE" && (!Id.HasValue || Id == Guid.Empty))
            yield return new ValidationResult("Thao tác MODIFY và DELETE yêu cầu ID thể thức hợp lệ.", [nameof(Id)]);
        if (Action == "DELETE")
            yield break;
        if (string.IsNullOrWhiteSpace(Code))
            yield return new ValidationResult("Mã thể thức là bắt buộc.", [nameof(Code)]);
        if (string.IsNullOrWhiteSpace(DisplayName))
            yield return new ValidationResult("Tên thể thức là bắt buộc.", [nameof(DisplayName)]);
        if (SideSize <= 0)
            yield return new ValidationResult("Số người mỗi bên phải lớn hơn 0.", [nameof(SideSize)]);
    }
}
