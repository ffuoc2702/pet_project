using MatchingSports.Shared;

namespace MatchingSports.Domain.ValueObjects;

public static class ResourceDescription
{
    public static string? Normalize(string? description)
    {
        var normalized = StringNormalizer.StartEndSpaceTrim(description);
        if (normalized?.Length > ResourceLimits.DescriptionMaxLength)
            throw new ArgumentException($"Mô tả không được vượt quá {ResourceLimits.DescriptionMaxLength} ký tự.", nameof(description));
        return normalized;
    }
}
