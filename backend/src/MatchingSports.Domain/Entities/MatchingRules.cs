using MatchingSports.Shared;

namespace MatchingSports.Domain.Entities;

public sealed class MatchingRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

internal static class MatchingRules
{
    public static void Require(bool condition, string message, string code = ErrorCodes.InvalidMatchingOperation)
    {
        if (!condition) throw new MatchingRuleException(code, message);
    }

    public static string Text(string? value, int maxLength, string name)
    {
        Require(!string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maxLength, $"{name} là bắt buộc và không được vượt quá {maxLength} ký tự.");
        return value!.Trim();
    }
}
