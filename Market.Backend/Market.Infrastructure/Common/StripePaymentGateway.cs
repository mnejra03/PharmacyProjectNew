using System.Net.Http.Headers;
using System.Text.Json;
using Market.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Market.Infrastructure.Common;

public sealed class StripePaymentGateway(HttpClient http, IConfiguration configuration) : IStripePaymentGateway
{
    private readonly string _secretKey = configuration["Stripe:SecretKey"] ?? string.Empty;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_secretKey)
        && _secretKey.StartsWith("sk_test_", StringComparison.Ordinal)
        && !string.IsNullOrWhiteSpace(PublishableKey)
        && PublishableKey.StartsWith("pk_test_", StringComparison.Ordinal);
    public string PublishableKey => configuration["Stripe:PublishableKey"] ?? string.Empty;

    public async Task<PaymentIntentResult> CreateIntentAsync(long amountMinor, string currency, string userId, CancellationToken ct)
    {
        EnsureConfigured();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/payment_intents");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(_secretKey + ":")));
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["amount"] = amountMinor.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["currency"] = currency.ToLowerInvariant(),
            ["payment_method_types[0]"] = "card",
            ["metadata[userId]"] = userId
        });
        using var response = await http.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(ReadStripeError(json));
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        return new(root.GetProperty("id").GetString()!, root.GetProperty("client_secret").GetString()!,
            root.GetProperty("amount").GetInt64(), root.GetProperty("currency").GetString()!, root.GetProperty("status").GetString()!,
            root.GetProperty("metadata").GetProperty("userId").GetString() ?? string.Empty);
    }

    public async Task<PaymentIntentResult?> GetIntentAsync(string paymentIntentId, CancellationToken ct)
    {
        EnsureConfigured();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.stripe.com/v1/payment_intents/{Uri.EscapeDataString(paymentIntentId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(_secretKey + ":")));
        using var response = await http.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(ReadStripeError(json));
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        return new(root.GetProperty("id").GetString()!, root.GetProperty("client_secret").GetString()!,
            root.GetProperty("amount").GetInt64(), root.GetProperty("currency").GetString()!, root.GetProperty("status").GetString()!,
            root.GetProperty("metadata").GetProperty("userId").GetString() ?? string.Empty);
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(PublishableKey) || !PublishableKey.StartsWith("pk_test_", StringComparison.Ordinal))
            throw new InvalidOperationException("Stripe test ključevi nisu podešeni. Postavite Stripe__SecretKey i Stripe__PublishableKey.");
    }

    private static string ReadStripeError(string body)
    {
        try { using var doc = JsonDocument.Parse(body); return doc.RootElement.GetProperty("error").GetProperty("message").GetString() ?? "Stripe zahtjev nije uspio."; }
        catch { return "Stripe zahtjev nije uspio."; }
    }
}
