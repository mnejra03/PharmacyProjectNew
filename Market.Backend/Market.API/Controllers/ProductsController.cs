using Market.Application.Modules.Catalog;
using Market.Application.Abstractions;

namespace Market.API.Controllers;

[ApiController, Route("api/products")]
public sealed class ProductsController(IMediator mediator, IFileStorage fileStorage, IAppCurrentUser currentUser) : ControllerBase
{
    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<PageResult<ProductDto>>> Get([FromQuery] ProductListQuery query, CancellationToken ct) => Ok(await mediator.Send(query, ct));

    [HttpGet("{id:int}"), AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct) => Ok(await mediator.Send(new ProductByIdQuery(id), ct));

    [HttpPost("image"), Authorize]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<UploadedProductImageDto>> UploadImage(IFormFile? file, CancellationToken ct)
    {
        if (!currentUser.IsAdmin && !currentUser.IsPharmacist) return Forbid();
        if (file is null || file.Length == 0 || file.Length > 5 * 1024 * 1024)
            return BadRequest("Odaberite sliku veličine do 5 MB.");
        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return BadRequest("Podržani formati su JPEG, PNG, WEBP i GIF.");
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        var content = stream.ToArray();
        var detectedType = DetectImageContentType(content);
        if (detectedType is null || !string.Equals(detectedType, file.ContentType, StringComparison.OrdinalIgnoreCase))
            return BadRequest("Sadržaj datoteke ne odgovara podržanom formatu slike.");
        try
        {
            var stored = await fileStorage.SaveAsync(content, detectedType, "product-images", ct);
            var imageUrl = Uri.TryCreate(stored.RelativeUrl, UriKind.Absolute, out var absolute)
                ? absolute.ToString()
                : $"{Request.Scheme}://{Request.Host}{stored.RelativeUrl}";
            return Ok(new UploadedProductImageDto(imageUrl));
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    private static string? DetectImageContentType(byte[] data)
    {
        var bytes = data.AsSpan();
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return "image/jpeg";
        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return "image/png";
        if (bytes.Length >= 6 && (bytes[..6].SequenceEqual("GIF87a"u8) || bytes[..6].SequenceEqual("GIF89a"u8))) return "image/gif";
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8)) return "image/webp";
        return null;
    }

    [HttpPost, Authorize]
    public async Task<ActionResult<ProductDto>> Create([FromBody] SaveProductCommand command, CancellationToken ct) => Ok(await mediator.Send(command with { Id = null }, ct));

    [HttpPut("{id:int}"), Authorize]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] SaveProductCommand command, CancellationToken ct) => Ok(await mediator.Send(command with { Id = id }, ct));

    [HttpPost("{id:int}/restock"), Authorize]
    public async Task<ActionResult<int>> Restock(int id, [FromBody] RestockProductBody body, CancellationToken ct) => Ok(await mediator.Send(new RestockProductCommand(id, body.Quantity), ct));

    [HttpDelete("{id:int}"), Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) { await mediator.Send(new DeleteProductCommand(id), ct); return NoContent(); }
}

public sealed record RestockProductBody(int Quantity);

public sealed record UploadedProductImageDto(string ImageUrl);

[ApiController, Route("api/categories")]
public sealed class CategoriesController(IMediator mediator) : ControllerBase
{
    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> Get(CancellationToken ct) => Ok(await mediator.Send(new GetCategoriesQuery(), ct));
}

[ApiController, Route("api/brands")]
public sealed class BrandsController(IMediator mediator) : ControllerBase
{
    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<BrandDto>>> Get(CancellationToken ct) => Ok(await mediator.Send(new GetBrandsQuery(), ct));
}
