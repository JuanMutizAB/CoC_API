using System.Net;
using CoC_API.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CoC_API.Integrations;

public sealed class EmailClient(EmailPublishingOptions options)
{
    public async Task<string> SendCampaignAsync(Campaign campaign, CancellationToken cancellationToken)
    {
        if (!options.IsConfigured)
            throw new InvalidOperationException("Email publishing is not fully configured.");

        var recipients = campaign.GetEmailRecipientList();
        if (recipients.Count == 0)
            recipients = options.RecipientList;
        if (recipients.Count == 0)
            throw new InvalidOperationException("Email campaign requires at least one recipient.");

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(options.From!));
        // Os destinatários vão em BCC para não expor os endereços uns aos outros.
        message.To.Add(MailboxAddress.Parse(options.From!));
        foreach (var recipient in recipients)
            message.Bcc.Add(MailboxAddress.Parse(recipient));

        message.MessageId = MimeKit.Utils.MimeUtils.GenerateMessageId();
        message.Subject = campaign.Name;

        var html = WebUtility.HtmlEncode(campaign.ModelMsg).Replace("\n", "<br>");
        if (Uri.TryCreate(campaign.ImageUrl, UriKind.Absolute, out var imageUri)
            && imageUri.Scheme == Uri.UriSchemeHttps)
        {
            html += $"<br><br><img src=\"{WebUtility.HtmlEncode(imageUri.AbsoluteUri)}\" style=\"max-width:100%\" alt=\"\">";
        }

        message.Body = new BodyBuilder
        {
            TextBody = campaign.ModelMsg,
            HtmlBody = html
        }.ToMessageBody();

        using var smtp = new SmtpClient();
        try
        {
            await smtp.ConnectAsync(
                options.Host!,
                options.PortNumber,
                options.PortNumber == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls,
                cancellationToken);
            await smtp.AuthenticateAsync(options.Username!, options.Password!, cancellationToken);
            await smtp.SendAsync(FormatOptions.Default, new MailKit.SendRequest(message), cancellationToken);
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException and not InvalidOperationException)
        {
            throw new InvalidOperationException($"SMTP error: {exception.Message}", exception);
        }
        finally
        {
            if (smtp.IsConnected)
                await smtp.DisconnectAsync(true, CancellationToken.None);
        }

        return message.MessageId ?? string.Empty;
    }
}
