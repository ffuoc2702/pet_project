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
        if (name is null)
            throw new ArgumentNullException(nameof(name), "Tên không được để trống.");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên không được để trống.", nameof(name));
        var normalizedName = name.Trim().ToUpperInvariant();
        var normalizedDescription = ResourceDescription.Normalize(description);
        if (normalizedName.Length > 100 || normalizedName.Any(character =>
            !char.IsAsciiLetterOrDigit(character) && character is not '_' and not '-'))
            throw new ArgumentException("Tên vai trò chỉ được chứa tối đa 100 chữ cái ASCII, chữ số, dấu gạch dưới hoặc dấu gạch ngang.", nameof(name));
        if (!isActive && isDefaultForRegistration)
            throw new ArgumentException("Không thể gán vai trò đã vô hiệu hóa cho người dùng mới.", nameof(isDefaultForRegistration));
        return new RoleDetails(normalizedName, normalizedDescription, isActive, isDefaultForRegistration);
    }
}
