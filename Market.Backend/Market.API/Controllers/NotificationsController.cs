using Market.Application.Modules.Communication.Notifications;
namespace Market.API.Controllers;
[ApiController, Route("api/notifications"), Authorize]
public sealed class NotificationsController(IMediator mediator) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<NotificationDto>>> Get(CancellationToken ct) => Ok(await mediator.Send(new GetNotificationsQuery(), ct));
    [HttpGet("admin")] public async Task<ActionResult<IReadOnlyList<AdminNotificationDto>>> GetAllForAdmin(CancellationToken ct) => Ok(await mediator.Send(new GetAllNotificationsForAdminQuery(), ct));
    [HttpPut("admin/{id:int}/read-state")] public async Task<IActionResult> SetAdminReadState(int id, [FromBody] SetAdminReadStateBody body, CancellationToken ct) { await mediator.Send(new SetAdminNotificationReadStateCommand(id, body.IsRead), ct); return NoContent(); }
    [HttpPut("{id:int}/read")] public async Task<IActionResult> MarkRead(int id, CancellationToken ct) { await mediator.Send(new MarkNotificationReadCommand(id), ct); return NoContent(); }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) { await mediator.Send(new DeleteNotificationCommand(id), ct); return NoContent(); }
}
public sealed record SetAdminReadStateBody(bool IsRead);
