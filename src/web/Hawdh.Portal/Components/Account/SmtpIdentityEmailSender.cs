using System.Globalization;
using System.Text.Encodings.Web;
using Hawdh.Portal.Data;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Hawdh.Portal.Components.Account;

public sealed class SmtpIdentityEmailSender(IOptions<SmtpEmailOptions> options) : IEmailSender<ApplicationUser>
{
    private readonly SmtpEmailOptions settings = options.Value;

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var isAddressChange = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);
        return isAddressChange
            ? SendLinkAsync(email, confirmationLink,
                "تأكيد تغيير البريد الإلكتروني — AGIRE TAIRET", "Confirmez le changement d’adresse e-mail — AGIRE TAIRET",
                "لتأكيد تغيير بريد حسابك إلى هذا العنوان، افتح الرابط التالي:", "Pour confirmer le changement de l’adresse e-mail de votre compte vers cette adresse, ouvrez le lien suivant :")
            : SendLinkAsync(email, confirmationLink,
                "تأكيد البريد الإلكتروني — AGIRE TAIRET", "Confirmez votre adresse e-mail — AGIRE TAIRET",
                "لتأكيد بريدك الإلكتروني، افتح الرابط التالي:", "Pour confirmer votre adresse e-mail, ouvrez le lien suivant :");
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendLinkAsync(email, resetLink,
            "استعادة كلمة المرور — AGIRE TAIRET", "Réinitialisation du mot de passe — AGIRE TAIRET",
            "لاستعادة كلمة المرور، افتح الرابط التالي. إذا لم تطلب ذلك فتجاهل هذه الرسالة:",
            "Pour réinitialiser votre mot de passe, ouvrez le lien suivant. Si vous n’êtes pas à l’origine de cette demande, ignorez ce message :");

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var arabic = IsArabic;
        return SendAsync(email,
            arabic ? "رمز استعادة كلمة المرور — AGIRE TAIRET" : "Code de réinitialisation — AGIRE TAIRET",
            arabic ? $"رمز استعادة كلمة المرور: {resetCode}" : $"Votre code de réinitialisation du mot de passe : {resetCode}");
    }

    private Task SendLinkAsync(string email, string link, string arabicSubject, string frenchSubject, string arabicText, string frenchText)
    {
        var arabic = IsArabic;
        var subject = arabic ? arabicSubject : frenchSubject;
        var intro = arabic ? arabicText : frenchText;
        var safeLink = HtmlEncoder.Default.Encode(link);
        var direction = arabic ? "rtl" : "ltr";
        var action = arabic ? "فتح الرابط" : "Ouvrir le lien";
        var html = $"<!doctype html><html lang=\"{(arabic ? "ar" : "fr")}\" dir=\"{direction}\"><body style=\"font-family:Arial,sans-serif;color:#222;line-height:1.8\"><p>{HtmlEncoder.Default.Encode(intro)}</p><p><a href=\"{safeLink}\">{action}</a></p><p style=\"direction:ltr;word-break:break-all\">{safeLink}</p><p>AGIRE TAIRET</p></body></html>";
        return SendAsync(email, subject, intro + Environment.NewLine + link, html);
    }

    private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase);

    private async Task SendAsync(string email, string subject, string textBody, string? htmlBody = null)
    {
        if (!settings.IsConfigured)
            throw new InvalidOperationException("Email delivery is not configured. Configure the SMTP host, sender address, port and TLS mode.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        var body = new BodyBuilder { TextBody = textBody, HtmlBody = htmlBody };
        message.Body = body.ToMessageBody();

        using var client = new SmtpClient { Timeout = 15000 };
        await client.ConnectAsync(settings.Host, settings.Port, settings.GetSecureSocketOptions());
        if (!string.IsNullOrWhiteSpace(settings.UserName))
            await client.AuthenticateAsync(settings.UserName, settings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
