using Market.Application.Modules.Communication.Notifications;
namespace Market.API.Controllers;
[ApiController, Route("api/notifications"), Authorize]
public sealed class NotificationsController(IMediator mediator) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<NotificationDto>>> Get(CancellationToken ct) => Ok(await mediator.Send(new GetNotificationsQuery(), ct));
    [HttpPut("{id:int}/read")] public async Task<IActionResult> MarkRead(int id, CancellationToken ct) { await mediator.Send(new MarkNotificationReadCommand(id), ct); return NoContent(); }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) { await mediator.Send(new DeleteNotificationCommand(id), ct); return NoContent(); }
}
