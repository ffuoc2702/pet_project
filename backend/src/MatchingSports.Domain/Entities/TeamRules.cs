using MatchingSports.Shared;

namespace MatchingSports.Domain.Entities;

public sealed class TeamRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public static class TeamRules
{
    public static void Require(bool condition, string message)
    {
        if (!condition) throw new TeamRuleException(ErrorCodes.InvalidTeamOperation, message);
    }
    public static void ValidateCapacity(Action action)
    {
        try { action(); }
        catch (ArgumentException exception) { throw new TeamRuleException(ErrorCodes.InvalidTeamOperation, exception.GetValidationMessage()); }
    }
    public static void RequireCaptainCapacity(int count, SystemSettings settings) =>
        ValidateCapacity(() => settings.EnsureCanCaptainAnotherTeam(count));
    public static void RequireMemberCapacity(int count, SystemSettings settings) =>
        ValidateCapacity(() => settings.EnsureCanJoinAnotherTeam(count, 0));
}
