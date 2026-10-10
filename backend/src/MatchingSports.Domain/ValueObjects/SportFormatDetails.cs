namespace MatchingSports.Domain.ValueObjects;

public sealed record SportFormatDetails
{
    private SportFormatDetails(Guid? id, string code, string displayName, short sideSize, string? description = null)
    {
        Id = id;
        Code = code;
        DisplayName = displayName;
        SideSize = sideSize;
        Description = description;
    }

    public string? Description { get; }
    public Guid? Id { get; }
    public string Code { get; }
    public string DisplayName { get; }
    public short SideSize { get; }
    public string Action { get; private init; } = "NEW";

    public static SportFormatDetails FromAction(string action, Guid? id, string? code, string? displayName, short sideSize, string? description = null)
    {
        if (action is not ("NEW" or "MODIFY" or "DELETE"))
            throw new ArgumentException("Thao tác với thể thức phải là NEW, MODIFY hoặc DELETE.", nameof(action));
        if (action == "NEW" && id.HasValue)
            throw new ArgumentException("Thể thức mới (NEW) không được có ID.", nameof(id));
        if (action != "NEW" && (!id.HasValue || id == Guid.Empty))
            throw new ArgumentException("Thao tác MODIFY và DELETE yêu cầu ID thể thức hợp lệ.", nameof(id));
        return action == "DELETE"
            ? new SportFormatDetails(id, string.Empty, string.Empty, 0) { Action = action }
            : Create(code!, displayName!, sideSize, id, description) with { Action = action };
    }

    public static SportFormatDetails Create(string code, string displayName, short sideSize, Guid? id = null, string? description = null)
    {
        if (code is null)
            throw new ArgumentNullException(nameof(code), "Mã không được để trống.");
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Mã không được để trống.", nameof(code));
        if (displayName is null)
            throw new ArgumentNullException(nameof(displayName), "Tên hiển thị không được để trống.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Tên hiển thị không được để trống.", nameof(displayName));
        if (id == Guid.Empty)
            throw new ArgumentException("ID thể thức không được là GUID rỗng.", nameof(id));
        var normalizedCode = code.Trim().ToUpperInvariant();
        var normalizedName = displayName.Trim();
        if (normalizedCode.Length > 32)
            throw new ArgumentException("Mã thể thức không được vượt quá 32 ký tự.", nameof(code));
        if (normalizedName.Length > 100)
            throw new ArgumentException("Tên thể thức không được vượt quá 100 ký tự.", nameof(displayName));
        if (sideSize <= 0)
            throw new ArgumentException("Số người mỗi bên phải lớn hơn 0.", nameof(sideSize));
        return new SportFormatDetails(id, normalizedCode, normalizedName, sideSize, ResourceDescription.Normalize(description)) { Action = id.HasValue ? "MODIFY" : "NEW" };
    }
}
