using System.Net;
using System.Net.Mail;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Features.Notification;
using MatchingSports.Infrastructure.Identity;
using MatchingSports.Shared;
using Microsoft.Extensions.Options;

namespace MatchingSports.Infrastructure.Notifications;

internal sealed class SmtpNotificationEmailSender(IOptions<AuthOptions> options) : INotificationEmailSender
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.Gmail.SenderEmail)
        && !string.IsNullOrWhiteSpace(options.Value.Gmail.Username) && !string.IsNullOrWhiteSpace(options.Value.Gmail.AppPassword);

    public async Task SendAsync(string email, string title, string html, string idempotencyKey, CancellationToken cancellationToken)
    {
        if (!IsConfigured) throw new NotificationDeliveryException(ErrorCodes.EmailNotConfigured);
        var gmail = options.Value.Gmail;
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(gmail.SenderEmail, gmail.SenderName),
                Subject = title,
                Body = html,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(email));
            // SMTP cannot guarantee deduplication. This header is only a stable correlation identifier.
            message.Headers.Add("X-Notification-Id", idempotencyKey);
            using var client = new SmtpClient(gmail.Host, gmail.Port)
            {
                EnableSsl = gmail.EnableSsl,
                Credentials = new NetworkCredential(gmail.Username, gmail.AppPassword)
            };
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(60));
            try { await client.SendMailAsync(message, timeout.Token); }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            { throw new NotificationDeliveryException(ErrorCodes.EmailSendFailed); }
        }
        catch (Exception exception) when (exception is SmtpException or FormatException or ArgumentException)
        { throw new NotificationDeliveryException(ErrorCodes.EmailSendFailed); }
    }
}
