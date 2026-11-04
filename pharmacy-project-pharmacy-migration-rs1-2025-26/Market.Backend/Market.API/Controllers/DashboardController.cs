using Market.Application.Modules.Accounts;
namespace Market.API.Controllers;
[ApiController, Route("api/dashboard"), Authorize]
public sealed class DashboardController(IMediator mediator) : ControllerBase
{ [HttpGet("stats")] public async Task<ActionResult<DashboardStatsDto>> Get(CancellationToken ct) => Ok(await mediator.Send(new GetDashboardStatsQuery(), ct)); }
