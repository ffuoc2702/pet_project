using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Infrastructure.Persistence;
using MatchingSports.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MatchingSports.Infrastructure.Identity;

internal sealed class IdentityService(
    UserManager<User> userManager,
    MatchingSportsDbContext db,
    IOtpEmailSender emailSender,
    IOptions<AuthOptions> options,
    TimeProvider timeProvider,
    IUserRoleRepository userRoleRepository,
    IAuditActorContext auditActorContext) : IIdentityService
{
    private readonly AuthOptions _options = options.Value;

    public async Task<RegistrationResult> RegisterAsync(
        string email,
        string password,
        string displayName,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        using var anonymousScope = auditActorContext.BeginScope(null);
        var canonicalEmail = CanonicalizeEmail(email);
        if (await userManager.FindByEmailAsync(canonicalEmail) is not null)
        {
            throw new ConflictException(
                ErrorCodes.EmailAlreadyRegistered,
                "Địa chỉ email đã được đăng ký.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = canonicalEmail,
            Email = canonicalEmail,
            EmailConfirmed = false,
            Status = UserStatus.ACTIVE,
            Profile = new UserProfile
            {
                DisplayName = displayName.Trim()
            }
        };

        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            ThrowIfFailed(await userManager.CreateAsync(user, password));
            await AssignRegistrationRolesAsync(user, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        var expiresAt = await CreateAndSendOtpAsync(
            user,
            OtpPurpose.EMAIL_VERIFICATION,
            ipAddress,
            cancellationToken);

        return new RegistrationResult(user.Id, canonicalEmail, expiresAt);
    }

    public async Task VerifyEmailAsync(
        string email,
        string otp,
        CancellationToken cancellationToken)
    {
        using var anonymousScope = auditActorContext.BeginScope(null);
        var user = await FindUserByEmailAsync(email);
        if (user.EmailConfirmed)
            return;

        var challenge = await ValidateOtpAsync(
            user,
            otp,
            OtpPurpose.EMAIL_VERIFICATION,
            cancellationToken);

        using var verifiedActorScope = auditActorContext.BeginScope(user.Id);
        challenge.VerifiedAt = timeProvider.GetUtcNow();
        user.EmailConfirmed = true;
        ThrowIfFailed(await userManager.UpdateAsync(user));
    }

    public async Task ResendVerificationOtpAsync(
        string email,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        using var anonymousScope = auditActorContext.BeginScope(null);
        var user = await userManager.FindByEmailAsync(CanonicalizeEmail(email));
        if (user is null || user.EmailConfirmed)
            return;

        await CreateAndSendOtpAsync(
            user,
            OtpPurpose.EMAIL_VERIFICATION,
            ipAddress,
            cancellationToken);
    }

    public async Task<AuthTokenResult> LoginAsync(
        string email,
        string password,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        using var anonymousScope = auditActorContext.BeginScope(null);
        var user = await userManager.FindByEmailAsync(CanonicalizeEmail(email));
        if (user is null)
            throw InvalidCredentials();

        if (await userManager.IsLockedOutAsync(user))
        {
            throw new ForbiddenException(
                ErrorCodes.AccountLocked,
                "Tài khoản tạm thời bị khóa do đăng nhập sai quá nhiều lần.");
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            throw InvalidCredentials();
        }

        using var verifiedActorScope = auditActorContext.BeginScope(user.Id);
        await userManager.ResetAccessFailedCountAsync(user);

        EnsureUserCanSignIn(user);
        if (!user.EmailConfirmed)
        {
            throw new UnauthorizedException(
                ErrorCodes.EmailNotVerified,
                "Địa chỉ email chưa được xác thực.");
        }

        return await IssueTokensAsync(user, ipAddress, userAgent, cancellationToken);
    }

    public async Task<AuthTokenResult> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        using var anonymousScope = auditActorContext.BeginScope(null);
        var now = timeProvider.GetUtcNow();
        var refreshTokenHash = HashRefreshToken(refreshToken);
        var session = await db.AuthSessions
            .Include(item => item.User)
            .SingleOrDefaultAsync(
                item => item.RefreshTokenHash == refreshTokenHash,
                cancellationToken);

        if (session is null || session.RevokedAt is not null || session.ExpiresAt <= now)
            throw InvalidRefreshToken();

        EnsureUserCanSignIn(session.User);
        if (!session.User.EmailConfirmed)
            throw InvalidRefreshToken();

        using var verifiedActorScope = auditActorContext.BeginScope(session.UserId);
        session.RevokedAt = now;
        return await IssueTokensAsync(session.User, ipAddress, userAgent, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var anonymousScope = auditActorContext.BeginScope(null);
        var refreshTokenHash = HashRefreshToken(refreshToken);
        var session = await db.AuthSessions.SingleOrDefaultAsync(
            item => item.RefreshTokenHash == refreshTokenHash,
            cancellationToken);

        if (session is null || session.RevokedAt is not null)
            return;

        using var verifiedActorScope = auditActorContext.BeginScope(session.UserId);
        session.RevokedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RequestPasswordResetAsync(
        string email,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        using var anonymousScope = auditActorContext.BeginScope(null);
        var user = await userManager.FindByEmailAsync(CanonicalizeEmail(email));
        if (user is null || !user.EmailConfirmed || user.Status != UserStatus.ACTIVE)
            return;

        await CreateAndSendOtpAsync(
            user,
            OtpPurpose.PASSWORD_RESET,
            ipAddress,
            cancellationToken);
    }

    public async Task ResetPasswordAsync(
        string email,
        string otp,
        string newPassword,
        CancellationToken cancellationToken)
    {
        using var anonymousScope = auditActorContext.BeginScope(null);
        var user = await FindUserByEmailAsync(email);
        var challenge = await ValidateOtpAsync(
            user,
            otp,
            OtpPurpose.PASSWORD_RESET,
            cancellationToken);

        using var verifiedActorScope = auditActorContext.BeginScope(user.Id);
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        ThrowIfFailed(await userManager.ResetPasswordAsync(user, resetToken, newPassword));

        challenge.VerifiedAt = timeProvider.GetUtcNow();
        foreach (var session in await db.AuthSessions
                    .Where(item => item.UserId == user.Id && item.RevokedAt == null)
                    .ToListAsync(cancellationToken))
        {
            session.RevokedAt = challenge.VerifiedAt;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthTokenResult> ExternalLoginAsync(
        string provider,
        string providerKey,
        string email,
        string displayName,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        using var anonymousScope = auditActorContext.BeginScope(null);
        var user = await userManager.FindByLoginAsync(provider, providerKey);
        var canonicalEmail = CanonicalizeEmail(email);

        if (user is null)
        {
            user = await userManager.FindByEmailAsync(canonicalEmail);
            if (user is null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = canonicalEmail,
                    Email = canonicalEmail,
                    EmailConfirmed = true,
                    Status = UserStatus.ACTIVE,
                    Profile = new UserProfile
                    {
                        DisplayName = NormalizeDisplayName(displayName, canonicalEmail)
                    }
                };
                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
                using var createdActorScope = auditActorContext.BeginScope(user.Id);
                ThrowIfFailed(await userManager.CreateAsync(user));
                await AssignRegistrationRolesAsync(user, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            else if (!user.EmailConfirmed)
            {
                using var verifiedActorScope = auditActorContext.BeginScope(user.Id);
                user.EmailConfirmed = true;
                ThrowIfFailed(await userManager.UpdateAsync(user));
            }

            using var linkedActorScope = auditActorContext.BeginScope(user.Id);
            ThrowIfFailed(await userManager.AddLoginAsync(
                user,
                new UserLoginInfo(provider, providerKey, provider)));
        }

        EnsureUserCanSignIn(user);
        return await IssueTokensAsync(user, ipAddress, userAgent, cancellationToken);
    }

    public async Task<CurrentUserResult> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(item => item.Profile)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.UserNotFound, "Không tìm thấy người dùng.");

        var authorization = await GetUserAuthorizationAsync(user.Id, cancellationToken);
        return ToCurrentUser(user, authorization.Roles, authorization.Permissions);
    }

    private async Task<DateTimeOffset> CreateAndSendOtpAsync(
        User user,
        OtpPurpose purpose,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = user.NormalizedEmail
            ?? userManager.NormalizeEmail(user.Email!);
        var now = timeProvider.GetUtcNow();
        var otpOptions = _options.Otp;

        var latest = await db.OtpChallenges
            .Where(item => item.NormalizedEmail == normalizedEmail && item.Purpose == purpose)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is not null
            && latest.CreatedAt.AddSeconds(otpOptions.ResendCooldownSeconds) > now)
        {
            throw new ConflictException(
                ErrorCodes.OtpResendTooSoon,
                "Vui lòng chờ trước khi yêu cầu gửi lại OTP.");
        }

        var sentInLastHour = await db.OtpChallenges.CountAsync(
            item => item.NormalizedEmail == normalizedEmail
                    && item.Purpose == purpose
                    && item.CreatedAt >= now.AddHours(-1),
            cancellationToken);
        if (sentInLastHour >= otpOptions.MaxSendsPerHour)
        {
            throw new ConflictException(
                ErrorCodes.OtpSendLimitReached,
                "Bạn đã yêu cầu quá nhiều mã OTP. Vui lòng thử lại sau.");
        }

        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000)
            .ToString("D6", CultureInfo.InvariantCulture);
        var challenge = new OtpChallenge
        {
            Id = Guid.NewGuid(),
            NormalizedEmail = normalizedEmail,
            Purpose = purpose,
            MaxAttempts = otpOptions.MaxAttempts,
            ExpiresAt = now.AddMinutes(otpOptions.LifetimeMinutes),
            RequestIp = ParseIpAddress(ipAddress)
        };
        challenge.OtpHash = HashOtp(challenge, otp);

        db.OtpChallenges.Add(challenge);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await emailSender.SendAsync(
                user.Email!,
                otp,
                purpose,
                TimeSpan.FromMinutes(otpOptions.LifetimeMinutes),
                cancellationToken);
        }
        catch
        {
            db.OtpChallenges.Remove(challenge);
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        return challenge.ExpiresAt;
    }

    private async Task<OtpChallenge> ValidateOtpAsync(
        User user,
        string otp,
        OtpPurpose purpose,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = user.NormalizedEmail
            ?? userManager.NormalizeEmail(user.Email!);
        var challenge = await db.OtpChallenges
            .Where(item => item.NormalizedEmail == normalizedEmail
                            && item.Purpose == purpose
                            && item.VerifiedAt == null)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();

        if (challenge is null
            || challenge.ExpiresAt <= now
            || challenge.AttemptCount >= challenge.MaxAttempts)
        {
            throw InvalidOtp();
        }

        var actualHash = HashOtp(challenge, otp);
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(challenge.OtpHash),
                Convert.FromHexString(actualHash)))
        {
            challenge.AttemptCount++;
            await db.SaveChangesAsync(cancellationToken);
            throw InvalidOtp();
        }

        return challenge;
    }

    private async Task<AuthTokenResult> IssueTokensAsync(
        User user,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        using var verifiedActorScope = auditActorContext.BeginScope(user.Id);
        var jwt = _options.Jwt;
        var now = timeProvider.GetUtcNow();
        var accessTokenExpiresAt = now.AddMinutes(jwt.AccessTokenMinutes);
        var refreshTokenExpiresAt = now.AddDays(jwt.RefreshTokenDays);
        var authorization = await GetUserAuthorizationAsync(user.Id, cancellationToken);
        var roles = authorization.Roles;
        var displayName = user.Profile?.DisplayName;
        if (displayName is null)
        {
            displayName = await db.UserProfiles
                .Where(profile => profile.UserId == user.Id)
                .Select(profile => profile.DisplayName)
                .SingleOrDefaultAsync(cancellationToken)
                ?? user.Email!;
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, displayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(authorization.Permissions.Select(permission => new Claim(AuthorizationClaimTypes.Permission, permission)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            jwt.Issuer,
            jwt.Audience,
            claims,
            now.UtcDateTime,
            accessTokenExpiresAt.UtcDateTime,
            credentials);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));

        db.AuthSessions.Add(new AuthSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RefreshTokenHash = HashRefreshToken(refreshToken),
            IpAddress = ParseIpAddress(ipAddress),
            UserAgent = string.IsNullOrWhiteSpace(userAgent)
                ? null
                : userAgent[..Math.Min(userAgent.Length, 500)],
            ExpiresAt = refreshTokenExpiresAt
        });
        user.LastLoginAt = now;
        await db.SaveChangesAsync(cancellationToken);

        return new AuthTokenResult(
            accessToken,
            refreshToken,
            "Bearer",
            accessTokenExpiresAt,
            refreshTokenExpiresAt,
            new CurrentUserResult(
                user.Id,
                user.Email!,
                displayName,
                user.EmailConfirmed,
                roles.ToArray(),
                authorization.Permissions));
    }

    private static CurrentUserResult ToCurrentUser(User user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions) => new(
        user.Id,
        user.Email!,
        user.Profile?.DisplayName ?? user.Email!,
        user.EmailConfirmed,
        roles,
        permissions);

    private async Task AssignRegistrationRolesAsync(User user, CancellationToken cancellationToken)
    {
        var roleIds = await db.Roles.Where(role => role.IsActive && role.IsDefaultForRegistration)
            .Select(role => role.Id).ToArrayAsync(cancellationToken);
        foreach (var roleId in roleIds)
            db.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = roleId });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<UserRoleAssignment> GetUserAuthorizationAsync(Guid userId, CancellationToken cancellationToken) =>
        await userRoleRepository.GetUserRolesAsync(userId, cancellationToken)
        ?? throw new ForbiddenException(ErrorCodes.AccountNotActive, "Tài khoản hiện không hoạt động.");

    private async Task<User> FindUserByEmailAsync(string email) =>
        await userManager.FindByEmailAsync(CanonicalizeEmail(email))
        ?? throw new NotFoundException(ErrorCodes.UserNotFound, "Không tìm thấy người dùng.");

    private string HashOtp(OtpChallenge challenge, string otp)
    {
        var payload = $"{challenge.Id:N}:{challenge.NormalizedEmail}:{challenge.Purpose}:{otp}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.Otp.Pepper));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    private static string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    private static IPAddress? ParseIpAddress(string? value) =>
        IPAddress.TryParse(value, out var address) ? address : null;

    private static string CanonicalizeEmail(string email) => email.Trim();

    private static string NormalizeDisplayName(string displayName, string email)
    {
        var value = string.IsNullOrWhiteSpace(displayName)
            ? email.Split('@', 2)[0]
            : displayName.Trim();
        return value[..Math.Min(value.Length, 100)];
    }

    private static void EnsureUserCanSignIn(User user)
    {
        if (user.Status != UserStatus.ACTIVE)
        {
            throw new ForbiddenException(
                ErrorCodes.AccountNotActive,
                "Tài khoản hiện không hoạt động.");
        }
    }

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (result.Succeeded)
            return;

        var errors = result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).Distinct().ToArray());
        throw new RequestValidationException(
            ErrorCodes.IdentityValidationFailed,
            "Thông tin tài khoản không đáp ứng yêu cầu bảo mật.",
            errors);
    }

    private static UnauthorizedException InvalidCredentials() => new(
        ErrorCodes.InvalidCredentials,
        "Email hoặc mật khẩu không chính xác.");

    private static UnauthorizedException InvalidRefreshToken() => new(
        ErrorCodes.InvalidRefreshToken,
        "Mã làm mới phiên đăng nhập không hợp lệ hoặc đã hết hạn.");

    private static UnauthorizedException InvalidOtp() => new(
        ErrorCodes.InvalidOtp,
        "Mã OTP không hợp lệ hoặc đã hết hạn.");
}
