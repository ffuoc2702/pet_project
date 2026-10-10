namespace MatchingSports.Domain.Entities;

public sealed class UserPreferences : ConcurrentAuditableEntity
{
    private UserPreferences() { }

    public Guid UserId { get; private set; }
    public string LanguageCode { get; private set; } = "vi";
    public string ColorMode { get; private set; } = "system";
    public string ThemePreset { get; private set; } = "blue";

    public static UserPreferences Create(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("Người dùng không hợp lệ.", nameof(userId));
        return new UserPreferences { UserId = userId };
    }

    public void Update(string languageCode, string colorMode, string themePreset)
    {
        if (languageCode is not ("vi" or "en"))
            throw new ArgumentException("Ngôn ngữ không được hỗ trợ.", nameof(languageCode));
        if (colorMode is not ("light" or "dark" or "system"))
            throw new ArgumentException("Chế độ nền không được hỗ trợ.", nameof(colorMode));
        if (themePreset is not ("blue" or "green" or "purple"))
            throw new ArgumentException("Bộ màu không được hỗ trợ.", nameof(themePreset));
        LanguageCode = languageCode;
        ColorMode = colorMode;
        ThemePreset = themePreset;
        RenewConcurrencyToken();
    }
}
