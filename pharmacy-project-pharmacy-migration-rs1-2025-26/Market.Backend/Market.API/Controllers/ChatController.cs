using Market.Application.Modules.Communication.Chat;
namespace Market.API.Controllers;
[ApiController, Route("api/chat"), Authorize]
public sealed class ChatController(IMediator mediator) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<ChatMessageDto>>> Get([FromQuery] int? otherUserId, CancellationToken ct) => Ok(await mediator.Send(new GetChatMessagesQuery(otherUserId), ct));
    [HttpGet("contacts")] public async Task<ActionResult<IReadOnlyList<ChatContactDto>>> Contacts(CancellationToken ct) => Ok(await mediator.Send(new GetChatContactsQuery(), ct));
    [HttpPost] public async Task<ActionResult<ChatMessageDto>> Send([FromBody] SendChatMessageCommand command, CancellationToken ct) => Ok(await mediator.Send(command, ct));
}
