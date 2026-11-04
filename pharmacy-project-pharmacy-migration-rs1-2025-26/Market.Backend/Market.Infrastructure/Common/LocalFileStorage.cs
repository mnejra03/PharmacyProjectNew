using Microsoft.AspNetCore.Hosting;
namespace Market.Infrastructure.Common;
public sealed class LocalFileStorage(IWebHostEnvironment environment) : IFileStorage
{
    private static readonly IReadOnlyDictionary<string, string> Extensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    { ["image/jpeg"] = ".jpg", ["image/png"] = ".png", ["image/webp"] = ".webp", ["image/gif"] = ".gif" };
    private string Root => Path.Combine(environment.ContentRootPath, "wwwroot", "uploads");

    public async Task<StoredFile> SaveAsync(byte[] content, string contentType, string containerName, CancellationToken ct)
    {
        if (!Extensions.TryGetValue(contentType, out var extension)) throw new InvalidOperationException("Format slike nije podržan.");
        var key = $"{Guid.NewGuid():N}{extension}";
        Directory.CreateDirectory(Root);
        await File.WriteAllBytesAsync(Path.Combine(Root, key), content, ct);
        return new StoredFile(key, $"/uploads/{key}", contentType);
    }

    public async Task<(byte[] Content, string ContentType)> ReadAsync(string key, CancellationToken ct)
    {
        if (Path.GetFileName(key) != key || key.Contains("..", StringComparison.Ordinal)) throw new FileNotFoundException();
        var path = Path.Combine(Root, key);
        var content = await File.ReadAllBytesAsync(path, ct);
        var contentType = Extensions.FirstOrDefault(x => x.Value.Equals(Path.GetExtension(key), StringComparison.OrdinalIgnoreCase)).Key ?? "application/octet-stream";
        return (content, contentType);
    }
}
