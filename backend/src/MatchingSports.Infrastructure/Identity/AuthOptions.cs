namespace MatchingSports.Infrastructure.Identity;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public JwtOptions Jwt { get; init; } = new();
    public OtpOptions Otp { get; init; } = new();
    public GoogleAuthOptions Google { get; init; } = new();
    public GmailOptions Gmail { get; init; } = new();
}

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "MatchingSports";
    public string Audience { get; init; } = "MatchingSports.Client";
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 30;
}

public sealed class OtpOptions
{
    public string Pepper { get; init; } = string.Empty;
    public int LifetimeMinutes { get; init; } = 10;
    public int ResendCooldownSeconds { get; init; } = 60;
    public int MaxSendsPerHour { get; init; } = 5;
    public short MaxAttempts { get; init; } = 5;
}

public sealed class GoogleAuthOptions
{
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string CallbackPath { get; init; } = "/signin-google";
}

public sealed class GmailOptions
{
    public string Host { get; init; } = "smtp.gmail.com";
    public int Port { get; init; } = 587;
    public bool EnableSsl { get; init; } = true;
    public string SenderName { get; init; } = "Matching Sports";
    public string SenderEmail { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string AppPassword { get; init; } = string.Empty;
}
