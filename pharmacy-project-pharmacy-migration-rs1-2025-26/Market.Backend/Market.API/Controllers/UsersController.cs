using Market.Application.Modules.Accounts;
namespace Market.API.Controllers;
[ApiController, Route("api/users"), Authorize]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet("me")] public async Task<ActionResult<UserProfileDto>> Me(CancellationToken ct) => Ok(await mediator.Send(new GetMyProfileQuery(), ct));
    [HttpPut("me")] public async Task<ActionResult<UserProfileDto>> UpdateMe([FromBody] UpdateMyProfileCommand command, CancellationToken ct) => Ok(await mediator.Send(command, ct));
    [HttpPost("me/password")] public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command, CancellationToken ct) { await mediator.Send(command, ct); return NoContent(); }
    [HttpPost("me/profile-image")] public async Task<ActionResult<UserProfileDto>> UpdateProfileImage(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0 || file.Length > 5 * 1024 * 1024) return BadRequest("Slika mora imati između 1 bajt i 5 MB.");
        await using var stream = new MemoryStream(); await file.CopyToAsync(stream, ct);
        var result = await mediator.Send(new UpdateProfileImageCommand(file.ContentType, stream.ToArray()), ct);
        var imageUrl = result.ProfileImageUrl is null ? null
            : Uri.TryCreate(result.ProfileImageUrl, UriKind.Absolute, out var absolute)
                ? absolute.ToString()
                : $"{Request.Scheme}://{Request.Host}{result.ProfileImageUrl}";
        return Ok(result with { ProfileImageUrl = imageUrl });
    }
    [HttpGet] public async Task<ActionResult<PageResult<UserProfileDto>>> Get([FromQuery] GetUsersQuery query, CancellationToken ct) => Ok(await mediator.Send(query, ct));
    [HttpPut("{id:int}")] public async Task<ActionResult<UserProfileDto>> Update(int id, [FromBody] UpdateUserCommand command, CancellationToken ct) => Ok(await mediator.Send(command with { Id = id }, ct));
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) { await mediator.Send(new DeleteUserCommand(id), ct); return NoContent(); }
}
