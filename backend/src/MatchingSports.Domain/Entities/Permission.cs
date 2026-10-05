namespace MatchingSports.Domain.Entities;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711", Justification = "Business permission entity; unrelated to legacy Code Access Security.")]
public sealed class Permission
{
    private readonly List<RolePermission> _rolePermissions = [];

    private Permission() { }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    public void UpdateDisplayName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalizedName = name.Trim();
        if (normalizedName.Length > 150)
            throw new ArgumentException("Permission name must contain at most 150 characters.", nameof(name));
        Name = normalizedName;
    }

    public static Permission Create(string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalizedCode = code.Trim().ToLowerInvariant();
        var normalizedName = name.Trim();
        if (normalizedCode.Length > 100 || normalizedCode.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not ('.' or '_' or '-')))
            throw new ArgumentException("Permission code must contain at most 100 ASCII letters, digits, dots, underscores or hyphens.", nameof(code));
        if (normalizedName.Length > 150)
            throw new ArgumentException("Permission name must contain at most 150 characters.", nameof(name));

        return new Permission { Id = Guid.NewGuid(), Code = normalizedCode, Name = normalizedName };
    }
}
