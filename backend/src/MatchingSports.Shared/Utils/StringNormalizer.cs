namespace MatchingSports.Shared;

public static class StringNormalizer
{
    public static string? StartEndSpaceTrim(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
