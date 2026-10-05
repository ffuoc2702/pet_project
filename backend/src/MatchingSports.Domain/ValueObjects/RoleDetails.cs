using MatchingSports.Shared;

namespace MatchingSports.Domain.ValueObjects;

public sealed record RoleDetails
{
    private RoleDetails(string name, string? description, bool isActive, bool isDefaultForRegistration)
    {
        Name = name;
        Description = description;
        IsActive = isActive;
        IsDefaultForRegistration = isDefaultForRegistration;
    }

    public string Name { get; }
    public string? Description { get; }
    public bool IsActive { get; }
    public bool IsDefaultForRegistration { get; }

    public static RoleDetails Create(string name, string? description, bool isActive, bool isDefaultForRegistration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalizedName = name.Trim().ToUpperInvariant();
        var normalizedDescription = StringNormalizer.StartEndSpaceTrim(description);
        if (normalizedName.Length > 100 || normalizedName.Any(character =>
            !char.IsAsciiLetterOrDigit(character) && character is not '_' and not '-'))
            throw new ArgumentException("Role name must contain at most 100 ASCII letters, digits, underscores or hyphens.", nameof(name));
        if (normalizedDescription?.Length > 500)
            throw new ArgumentException("Role description must contain at most 500 characters.", nameof(description));
        if (!isActive && isDefaultForRegistration)
            throw new ArgumentException("An inactive role cannot be assigned to new users.", nameof(isDefaultForRegistration));
        return new RoleDetails(normalizedName, normalizedDescription, isActive, isDefaultForRegistration);
    }
}
