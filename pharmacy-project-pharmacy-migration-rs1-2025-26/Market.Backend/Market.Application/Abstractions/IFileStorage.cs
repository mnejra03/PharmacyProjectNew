namespace Market.Application.Abstractions;
public sealed record StoredFile(string Key, string RelativeUrl, string ContentType);
public interface IFileStorage
{
    Task<StoredFile> SaveAsync(byte[] content, string contentType, string containerName, CancellationToken ct);
    Task<(byte[] Content, string ContentType)> ReadAsync(string key, CancellationToken ct);
}

public sealed record PaymentIntentResult(string Id, string ClientSecret, long AmountMinor, string Currency, string Status, string UserId);
public interface IStripePaymentGateway
{
    bool IsConfigured { get; }
    string PublishableKey { get; }
    Task<PaymentIntentResult> CreateIntentAsync(long amountMinor, string currency, string userId, CancellationToken ct);
    Task<PaymentIntentResult?> GetIntentAsync(string paymentIntentId, CancellationToken ct);
}
