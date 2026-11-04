using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Market.Infrastructure.Common;

public sealed class SmtpPasswordResetEmailSender(
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<SmtpPasswordResetEmailSender> logger) : IPasswordResetEmailSender
{
    public async Task SendAsync(string email, string token, CancellationToken cancellationToken)
    {
        var frontendUrl = (configuration["Frontend:BaseUrl"] ?? "http://localhost:4200").TrimEnd('/');
        var resetUrl = $"{frontendUrl}/auth/reset-password?token={Uri.EscapeDataString(token)}";
        var host = configuration["Smtp:Host"];
        var from = configuration["Smtp:From"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            if (environment.IsDevelopment())
                logger.LogWarning("SMTP is not configured. Development password reset link: {ResetUrl}", resetUrl);
            else
                logger.LogError("SMTP is not configured; a password reset email could not be sent.");
            return;
        }

        try
        {
            using var message = new MailMessage(from, email)
            {
                Subject = "Reset lozinke za Pharmacy",
                IsBodyHtml = true,
                Body = $"<p>Primili smo zahtjev za promjenu lozinke.</p><p><a href=\"{HtmlEncoder.Default.Encode(resetUrl)}\">Postavi novu lozinku</a></p><p>Link važi 30 minuta i može se iskoristiti samo jednom. Ako nisi tražila reset, zanemari ovu poruku.</p>"
            };

            var port = int.TryParse(configuration["Smtp:Port"], out var configuredPort) ? configuredPort : 587;
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = !bool.TryParse(configuration["Smtp:EnableSsl"], out var enabled) || enabled,
                Timeout = 15000
            };
            var username = configuration["Smtp:Username"];
            var password = configuration["Smtp:Password"];
            if (!string.IsNullOrWhiteSpace(username))
                client.Credentials = new NetworkCredential(username, password ?? string.Empty);

            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception exception)
        {
            // Keep the public response generic to avoid revealing whether an account exists.
            logger.LogError(exception, "Failed to deliver a password reset email.");
        }
    }
}
