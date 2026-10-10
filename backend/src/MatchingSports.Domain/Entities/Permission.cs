using MatchingSports.Domain.ValueObjects;

namespace MatchingSports.Domain.Entities;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711", Justification = "Business permission entity; unrelated to legacy Code Access Security.")]
public sealed class Permission : AuditableEntity
{
    private readonly List<RolePermission> _rolePermissions = [];

    private Permission() { }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Name { get; private set; } = null!;
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    public void UpdateDisplayName(string name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name), "Tên không được để trống.");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên không được để trống.", nameof(name));
        var normalizedName = name.Trim();
        if (normalizedName.Length > 150)
            throw new ArgumentException("Tên quyền không được vượt quá 150 ký tự.", nameof(name));
        Name = normalizedName;
    }

    public void UpdateDescription(string? description) => Description = ResourceDescription.Normalize(description);

    public static Permission Create(string code, string name, string? description = null)
    {
        if (code is null)
            throw new ArgumentNullException(nameof(code), "Mã không được để trống.");
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Mã không được để trống.", nameof(code));
        if (name is null)
            throw new ArgumentNullException(nameof(name), "Tên không được để trống.");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên không được để trống.", nameof(name));
        var normalizedCode = code.Trim().ToLowerInvariant();
        var normalizedName = name.Trim();
        if (normalizedCode.Length > 100 || normalizedCode.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not ('.' or '_' or '-')))
            throw new ArgumentException("Mã quyền chỉ được chứa tối đa 100 chữ cái ASCII, chữ số, dấu chấm, dấu gạch dưới hoặc dấu gạch ngang.", nameof(code));
        if (normalizedName.Length > 150)
            throw new ArgumentException("Tên quyền không được vượt quá 150 ký tự.", nameof(name));

        return new Permission { Id = Guid.NewGuid(), Code = normalizedCode, Name = normalizedName, Description = ResourceDescription.Normalize(description) };
    }
}
