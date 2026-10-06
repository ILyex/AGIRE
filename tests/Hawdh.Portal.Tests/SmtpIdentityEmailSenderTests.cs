using System.Net;
using System.Net.Sockets;
using System.Text;
using Hawdh.Portal.Components.Account;
using Hawdh.Portal.Data;
using Microsoft.Extensions.Options;

namespace Hawdh.Portal.Tests;

public sealed class SmtpIdentityEmailSenderTests
{
    [Theory]
    [InlineData("StartTls")]
    [InlineData("SslOnConnect")]
    public void Production_smtp_settings_require_a_valid_sender_and_tls(string security)
    {
        var options = new SmtpEmailOptions
        {
            Host = "smtp.example.test",
            Port = 587,
            Security = security,
            FromAddress = "no-reply@example.test"
        };

        Assert.True(options.IsProductionReady);
    }

    [Fact]
    public void Production_rejects_unencrypted_or_incomplete_smtp_settings()
    {
        var options = new SmtpEmailOptions
        {
            Host = "smtp.example.test",
            Port = 25,
            Security = "None",
            FromAddress = "no-reply@example.test"
        };
        Assert.True(options.IsConfigured);
        Assert.False(options.IsProductionReady);

        options.Security = "StartTls";
        options.UserName = "smtp-user";
        Assert.False(options.IsProductionReady);
    }

    [Fact]
    public async Task Sends_a_confirmation_message_with_a_working_html_link_over_smtp()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var received = ServeOneMessageAsync(listener);
        var sender = new SmtpIdentityEmailSender(Options.Create(new SmtpEmailOptions
        {
            Host = "127.0.0.1",
            Port = port,
            Security = "None",
            FromAddress = "no-reply@example.test",
            FromName = "AGIRE TAIRET"
        }));

        await sender.SendConfirmationLinkAsync(new ApplicationUser(), "employee@example.test", "https://platform.example.test/confirm?code=abc123");
        var message = await received.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Contains("employee@example.test", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no-reply@example.test", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://platform.example.test/confirm?code=abc123", message, StringComparison.Ordinal);
        Assert.Contains("AGIRE TAIRET", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refuses_to_report_delivery_when_smtp_is_not_configured()
    {
        var sender = new SmtpIdentityEmailSender(Options.Create(new SmtpEmailOptions()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendConfirmationLinkAsync(
            new ApplicationUser(), "employee@example.test", "https://platform.example.test/confirm"));
    }

    private static async Task<string> ServeOneMessageAsync(TcpListener listener)
    {
        using var client = await listener.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(5));
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        await using var writer = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };
        var message = new StringBuilder();

        await writer.WriteLineAsync("220 localhost ready");
        while (await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)) is { } command)
        {
            if (command.StartsWith("EHLO ", StringComparison.OrdinalIgnoreCase) || command.StartsWith("HELO ", StringComparison.OrdinalIgnoreCase))
                await writer.WriteLineAsync("250 localhost");
            else if (command.StartsWith("MAIL FROM:", StringComparison.OrdinalIgnoreCase)
                || command.StartsWith("RCPT TO:", StringComparison.OrdinalIgnoreCase))
                await writer.WriteLineAsync("250 accepted");
            else if (command.Equals("DATA", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("354 end with dot");
                while (await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)) is { } line && line != ".")
                    message.AppendLine(line);
                await writer.WriteLineAsync("250 queued");
            }
            else if (command.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("221 bye");
                break;
            }
            else
                await writer.WriteLineAsync("250 accepted");
        }

        return message.ToString();
    }
}
