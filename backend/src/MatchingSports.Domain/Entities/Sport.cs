using MatchingSports.Domain.ValueObjects;

namespace MatchingSports.Domain.Entities;

public sealed class Sport : AuditableEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string? Description { get; private set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;

    public static Sport Create(string code, string name, IReadOnlyCollection<SportFormatDetails> formats, string? description = null)
    {
        var sport = new Sport { Id = Guid.NewGuid() };
        if (formats is null)
            throw new ArgumentNullException(nameof(formats), "Danh sách thể thức không được là null.");
        if (formats.Any(format => format is null || format.Action != "NEW"))
            throw new ArgumentException("Khi tạo môn thể thao, chỉ chấp nhận thể thức có thao tác NEW.", nameof(formats));
        sport.UpdateDetails(code, name, formats, description);
        return sport;
    }

    public void UpdateDetails(string code, string name, IReadOnlyCollection<SportFormatDetails> formats, string? description = null)
    {
        if (code is null)
            throw new ArgumentNullException(nameof(code), "Mã không được để trống.");
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Mã không được để trống.", nameof(code));
        if (name is null)
            throw new ArgumentNullException(nameof(name), "Tên không được để trống.");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên không được để trống.", nameof(name));
        var normalizedCode = code.Trim().ToUpperInvariant();
        var normalizedName = name.Trim();
        if (normalizedCode.Length > 32)
            throw new ArgumentException("Mã môn thể thao không được vượt quá 32 ký tự.", nameof(code));
        if (normalizedName.Length > 100)
            throw new ArgumentException("Tên môn thể thao không được vượt quá 100 ký tự.", nameof(name));

        var normalizedDescription = ResourceDescription.Normalize(description);
        var updates = PrepareFormatUpdates(formats);
        Description = normalizedDescription;
        Code = normalizedCode;
        Name = normalizedName;
        foreach (var (format, details, isNew) in updates)
        {
            if (details.Action == "DELETE")
                format.Deactivate();
            else
                format.UpdateDetails(details);
            if (isNew)
                Formats.Add(format);
        }
    }

    private List<(SportFormat Format, SportFormatDetails Details, bool IsNew)> PrepareFormatUpdates(
        IReadOnlyCollection<SportFormatDetails> formats, string? description = null)
    {
        if (formats is null)
            throw new ArgumentNullException(nameof(formats), "Danh sách thể thức không được là null.");
        if (formats.Count == 0 || formats.Any(format => format is null))
            throw new ArgumentException("Phải có ít nhất một thể thức hợp lệ.", nameof(formats));

        var updates = new List<(SportFormat Format, SportFormatDetails Details, bool IsNew)>();
        var includedIds = new HashSet<Guid>();
        foreach (var details in formats)
        {
            var existing = details.Id.HasValue
                ? Formats.SingleOrDefault(format => format.Id == details.Id.Value)
                : null;
            if (details.Id.HasValue && existing is null)
                throw new ArgumentException("ID thể thức không thuộc môn thể thao này.", nameof(formats));
            var format = existing ?? SportFormat.Create(Id, details);
            if (!includedIds.Add(format.Id))
                throw new ArgumentException("Mỗi thể thức chỉ được xuất hiện một lần trong danh sách.", nameof(formats));
            updates.Add((format, details, existing is null));
        }
        // Deleted formats retain their identity and references, so their code/name remain reserved.
        var modifiedDetails = updates.Where(update => !update.IsNew && update.Details.Action == "MODIFY")
            .ToDictionary(update => update.Format.Id, update => update.Details);
        var finalDetails = Formats.Select(format => modifiedDetails.TryGetValue(format.Id, out var details)
                ? details
                : SportFormatDetails.Create(format.Code, format.DisplayName, format.SideSize, format.Id))
            .Concat(updates.Where(update => update.IsNew).Select(update => update.Details)).ToArray();
        if (finalDetails.Select(format => format.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() != finalDetails.Length)
            throw new ArgumentException("Mã thể thức không được trùng trong cùng một môn thể thao.", nameof(formats));
        if (finalDetails.Select(format => format.DisplayName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != finalDetails.Length)
            throw new ArgumentException("Tên thể thức không được trùng trong cùng một môn thể thao.", nameof(formats));
        if (!Formats.Any(format => format.IsActive && !updates.Any(update => update.Format.Id == format.Id))
            && !updates.Any(update => update.Details.Action != "DELETE"))
            throw new ArgumentException("Phải còn ít nhất một thể thức đang hoạt động.", nameof(formats));
        return updates;
    }

    public void Deactivate() => IsActive = false;

    public ICollection<SportFormat> Formats { get; set; } = [];
    public ICollection<UserSportProfile> UserSportProfiles { get; set; } = [];
    public ICollection<Team> Teams { get; set; } = [];
    public ICollection<Listing> Listings { get; set; } = [];
}
