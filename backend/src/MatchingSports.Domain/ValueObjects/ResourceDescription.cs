using MatchingSports.Shared;

namespace MatchingSports.Domain.ValueObjects;

public static class ResourceDescription
{
    public static string? Normalize(string? description, int maxLength = 2000)
    {
        var normalized = StringNormalizer.StartEndSpaceTrim(description);
        if (normalized?.Length > maxLength)
            throw new ArgumentException($"Mô tả không được vượt quá {maxLength} ký tự.", nameof(description));
        return normalized;
    }
}
