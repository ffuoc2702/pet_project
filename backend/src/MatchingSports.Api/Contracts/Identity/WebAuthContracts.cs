namespace MatchingSports.Api.Contracts;

public sealed record WebCsrfResponse(string RequestToken);
public sealed record WebAuthTokenResponse(string AccessToken, string TokenType,
    DateTimeOffset AccessTokenExpiresAt, CurrentUserResponse User);
