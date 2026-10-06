using MailKit.Security;
using MimeKit;

namespace Hawdh.Portal.Components.Account;

public sealed class SmtpEmailOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Security { get; set; } = "StartTls";
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "AGIRE TAIRET";

    public bool UsesTls => string.Equals(Security, "StartTls", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Security, "SslOnConnect", StringComparison.OrdinalIgnoreCase);

    public bool IsProductionReady => IsConfigured && UsesTls;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(FromAddress)
        && Port is > 0 and <= 65535
        && TryGetSecureSocketOptions(Security, out _)
        && (string.IsNullOrWhiteSpace(UserName) == string.IsNullOrWhiteSpace(Password))
        && MailboxAddress.TryParse(FromAddress, out _);

    public SecureSocketOptions GetSecureSocketOptions()
    {
        if (!TryGetSecureSocketOptions(Security, out var options))
            throw new InvalidOperationException("Smtp:Security must be StartTls or SslOnConnect.");
        return options;
    }

    private static bool TryGetSecureSocketOptions(string? security, out SecureSocketOptions options)
    {
        if (string.Equals(security, "StartTls", StringComparison.OrdinalIgnoreCase))
        {
            options = SecureSocketOptions.StartTls;
            return true;
        }
        if (string.Equals(security, "SslOnConnect", StringComparison.OrdinalIgnoreCase))
        {
            options = SecureSocketOptions.SslOnConnect;
            return true;
        }
        if (string.Equals(security, "None", StringComparison.OrdinalIgnoreCase))
        {
            options = SecureSocketOptions.None;
            return true;
        }
        options = default;
        return false;
    }
}
