using Market.Application.Modules.Catalog;
using Market.Application.Abstractions;

namespace Market.API.Controllers;

[ApiController, Route("api/products")]
public sealed class ProductsController(IMediator mediator, IFileStorage fileStorage) : ControllerBase
{
    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<PageResult<ProductDto>>> Get([FromQuery] ProductListQuery query, CancellationToken ct) => Ok(await mediator.Send(query, ct));

    [HttpGet("{id:int}"), AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct) => Ok(await mediator.Send(new ProductByIdQuery(id), ct));

    [HttpPost("image"), Authorize]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<UploadedProductImageDto>> UploadImage(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0 || file.Length > 5 * 1024 * 1024)
            return BadRequest("Odaberite sliku veličine do 5 MB.");
        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return BadRequest("Podržani formati su JPEG, PNG, WEBP i GIF.");
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        try
        {
            var stored = await fileStorage.SaveAsync(stream.ToArray(), file.ContentType, "product-images", ct);
            var imageUrl = Uri.TryCreate(stored.RelativeUrl, UriKind.Absolute, out var absolute)
                ? absolute.ToString()
                : $"{Request.Scheme}://{Request.Host}{stored.RelativeUrl}";
            return Ok(new UploadedProductImageDto(imageUrl));
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
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
