using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Market.Application.Abstractions;

namespace Market.Infrastructure.Common;

public sealed class AzureBlobFileStorage(BlobServiceClient blobs) : IFileStorage
{
    private static readonly IReadOnlyDictionary<string, string> Extensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif"
    };

    private static readonly HashSet<string> AllowedContainers = new(StringComparer.OrdinalIgnoreCase)
    {
        "product-images", "advertisement-images", "profile-images", "brand-images"
    };

    public async Task<StoredFile> SaveAsync(byte[] content, string contentType, string containerName, CancellationToken ct)
    {
        if (!Extensions.TryGetValue(contentType, out var extension))
            throw new InvalidOperationException("Format slike nije podržan.");
        if (!AllowedContainers.Contains(containerName))
            throw new InvalidOperationException("Nepoznat Azure Blob kontejner.");

        var blobName = $"{Guid.NewGuid():N}{extension}";
        var blob = blobs.GetBlobContainerClient(containerName).GetBlobClient(blobName);
        await blob.UploadAsync(BinaryData.FromBytes(content), new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType,
                CacheControl = "public, max-age=31536000"
            }
        }, ct);

        return new StoredFile($"{containerName}/{blobName}", blob.Uri.ToString(), contentType);
    }

    public async Task<(byte[] Content, string ContentType)> ReadAsync(string key, CancellationToken ct)
    {
        var parts = key.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !AllowedContainers.Contains(parts[0]) || parts[1].Contains("..", StringComparison.Ordinal))
            throw new FileNotFoundException();

        var response = await blobs.GetBlobContainerClient(parts[0]).GetBlobClient(parts[1]).DownloadContentAsync(ct);
        return (response.Value.Content.ToArray(), response.Value.Details.ContentType ?? "application/octet-stream");
    }
}
