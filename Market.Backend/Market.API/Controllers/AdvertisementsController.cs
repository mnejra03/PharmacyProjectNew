using Market.Application.Modules.Content.Advertisements;
namespace Market.API.Controllers;
[ApiController, Route("api/advertisements")]
public sealed class AdvertisementsController(IMediator mediator) : ControllerBase
{
    [HttpPost("image"), Authorize] public async Task<ActionResult<UploadedImageDto>> UploadImage(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0 || file.Length > 5 * 1024 * 1024) return BadRequest("Slika mora imati između 1 bajt i 5 MB.");
        await using var stream = new MemoryStream(); await file.CopyToAsync(stream, ct);
        var result = await mediator.Send(new UploadAdvertisementImageCommand(file.ContentType, stream.ToArray()), ct);
        var imageUrl = Uri.TryCreate(result.RelativeUrl, UriKind.Absolute, out var absolute)
            ? absolute.ToString()
            : $"{Request.Scheme}://{Request.Host}{result.RelativeUrl}";
        return Ok(result with { RelativeUrl = imageUrl });
    }
    [HttpGet, AllowAnonymous] public async Task<ActionResult<IReadOnlyList<AdvertisementDto>>> Get(CancellationToken ct) => Ok(await mediator.Send(new GetAdvertisementsQuery(), ct));
    [HttpPost, Authorize] public async Task<ActionResult<AdvertisementDto>> Create([FromBody] SaveAdvertisementCommand command, CancellationToken ct) => Ok(await mediator.Send(command, ct));
    [HttpPut("{id:int}"), Authorize] public async Task<ActionResult<AdvertisementDto>> Update(int id, [FromBody] SaveAdvertisementCommand command, CancellationToken ct) => Ok(await mediator.Send(command with { Id = id }, ct));
    [HttpDelete("{id:int}"), Authorize] public async Task<IActionResult> Delete(int id, CancellationToken ct) { await mediator.Send(new DeleteAdvertisementCommand(id), ct); return NoContent(); }
}
