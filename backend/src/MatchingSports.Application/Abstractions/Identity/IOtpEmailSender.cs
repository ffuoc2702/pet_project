using MatchingSports.Domain.Enums;

namespace MatchingSports.Application.Abstractions;

public interface IOtpEmailSender
{
    Task SendAsync(
        string recipientEmail,
        string otp,
        OtpPurpose purpose,
        TimeSpan validFor,
        CancellationToken cancellationToken);
}
