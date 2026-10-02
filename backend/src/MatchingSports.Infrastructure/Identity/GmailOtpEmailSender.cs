using System.Net;
using System.Net.Mail;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatchingSports.Infrastructure.Identity;

internal sealed partial class GmailOtpEmailSender(
    IOptions<AuthOptions> options,
    ILogger<GmailOtpEmailSender> logger) : IOtpEmailSender
{
    public async Task SendAsync(
        string recipientEmail,
        string otp,
        OtpPurpose purpose,
        TimeSpan validFor,
        CancellationToken cancellationToken)
    {
        var gmail = options.Value.Gmail;
        if (string.IsNullOrWhiteSpace(gmail.SenderEmail)
            || string.IsNullOrWhiteSpace(gmail.Username)
            || string.IsNullOrWhiteSpace(gmail.AppPassword))
        {
            throw new ServiceUnavailableException(
                ErrorCodes.EmailNotConfigured,
                "Dịch vụ gửi email chưa được cấu hình.");
        }

        var purposeText = purpose == OtpPurpose.EMAIL_VERIFICATION
            ? "xác thực địa chỉ email"
            : "đặt lại mật khẩu";

        using var message = new MailMessage
        {
            From = new MailAddress(gmail.SenderEmail, gmail.SenderName),
            Subject = $"Mã OTP {purposeText} - Matching Sports",
            Body = $"""
                <html><body>
                <h2>Matching Sports</h2>
                <p>Mã OTP để {purposeText} của bạn là:</p>
                <p style="font-size:28px;font-weight:bold;letter-spacing:6px">{otp}</p>
                <p>Mã có hiệu lực trong {validFor.TotalMinutes:0} phút. Không chia sẻ mã này với bất kỳ ai.</p>
                </body></html>
                """,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(recipientEmail));

        using var client = new SmtpClient(gmail.Host, gmail.Port)
        {
            EnableSsl = gmail.EnableSsl,
            Credentials = new NetworkCredential(gmail.Username, gmail.AppPassword)
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SmtpException exception)
        {
            LogSendFailed(logger, recipientEmail, exception);
            throw new ServiceUnavailableException(
                ErrorCodes.EmailSendFailed,
                "Không thể gửi email OTP. Vui lòng thử lại sau.");
        }
    }

    [LoggerMessage(2101, LogLevel.Error, "Failed to send OTP email to {RecipientEmail}")]
    private static partial void LogSendFailed(ILogger logger, string recipientEmail, Exception exception);
}
