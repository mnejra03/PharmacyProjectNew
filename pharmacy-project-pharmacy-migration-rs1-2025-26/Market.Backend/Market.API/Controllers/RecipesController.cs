using Market.Application.Modules.Content.Recipes;
namespace Market.API.Controllers;
[ApiController, Route("api/recipes"), Authorize]
public sealed class RecipesController(IMediator mediator) : ControllerBase
{
    [HttpPost] public async Task<ActionResult<RecipeDto>> Create([FromForm] RecipeForm form, CancellationToken ct)
    {
        if (form.Scan is { Length: > 10 * 1024 * 1024 }) return BadRequest("Sken može imati najviše 10 MB.");
        byte[]? bytes = null; if (form.Scan is { Length: > 0 }) { await using var ms = new MemoryStream(); await form.Scan.CopyToAsync(ms, ct); bytes = ms.ToArray(); }
        return Ok(await mediator.Send(new CreateRecipeCommand { DoctorFirstName = form.DoctorFirstName, DoctorLastName = form.DoctorLastName, Scan = bytes }, ct));
    }
    [HttpGet("my")] public async Task<ActionResult<IReadOnlyList<RecipeDto>>> Mine(CancellationToken ct) => Ok(await mediator.Send(new GetRecipesQuery(true), ct));
    [HttpGet] public async Task<ActionResult<IReadOnlyList<RecipeDto>>> All(CancellationToken ct) => Ok(await mediator.Send(new GetRecipesQuery(false), ct));
    [HttpPut("{id:int}/status")] public async Task<IActionResult> Status(int id, [FromBody] UpdateRecipeStatusCommand body, CancellationToken ct) { await mediator.Send(body with { Id = id }, ct); return NoContent(); }
    [HttpGet("{id:int}/scan")] public async Task<IActionResult> Download(int id, CancellationToken ct) { var file = await mediator.Send(new GetRecipeScanQuery(id), ct); return File(file.Content, file.ContentType, $"recipe-{id}.bin"); }
}
public sealed class RecipeForm { public string DoctorFirstName { get; set; } = string.Empty; public string DoctorLastName { get; set; } = string.Empty; public IFormFile? Scan { get; set; } }
