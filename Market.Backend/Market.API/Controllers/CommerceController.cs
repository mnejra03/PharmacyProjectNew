using Market.Application.Modules.Catalog;

namespace Market.API.Controllers;

[ApiController, Route("api/cart"), Authorize]
public sealed class CartController(IMediator mediator) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<CartDto>> Get(CancellationToken ct) => Ok(await mediator.Send(new GetCartQuery(), ct));
    [HttpPost("items")] public async Task<ActionResult<CartDto>> Add([FromBody] AddCartItemCommand command, CancellationToken ct) => Ok(await mediator.Send(command, ct));
    [HttpPut("items/{id:int}")] public async Task<ActionResult<CartDto>> Update(int id, [FromBody] UpdateCartItemCommand command, CancellationToken ct) => Ok(await mediator.Send(command with { Id = id }, ct));
    [HttpDelete("items/{id:int}")] public async Task<IActionResult> Remove(int id, CancellationToken ct) { await mediator.Send(new RemoveCartItemCommand(id), ct); return NoContent(); }
    [HttpPost("checkout")] public async Task<ActionResult<OrderDto>> Checkout([FromBody] CheckoutCommand command, CancellationToken ct) => Ok(await mediator.Send(command, ct));
}

[ApiController, Route("api/payments"), Authorize]
public sealed class PaymentsController(IMediator mediator) : ControllerBase
{
    [HttpPost("intent")]
    public async Task<ActionResult<CartPaymentIntentDto>> CreateIntent(CancellationToken ct) => Ok(await mediator.Send(new CreateCartPaymentIntentCommand(), ct));
}

[ApiController, Route("api/orders"), Authorize]
public sealed class OrdersController(IMediator mediator) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<OrderDto>>> Get([FromQuery] bool all = false, CancellationToken ct = default) => Ok(await mediator.Send(new GetOrdersQuery(all), ct));
    [HttpPut("{id:int}/status")] public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusCommand command, CancellationToken ct) { await mediator.Send(command with { Id = id }, ct); return NoContent(); }
}

[ApiController, Route("api/supply-orders"), Authorize]
public sealed class SupplyOrdersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupplyOrderDto>>> Get([FromQuery] bool all = false, CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetSupplyOrdersQuery(all), ct));

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateSupplyOrderCommand command, CancellationToken ct) =>
        Ok(await mediator.Send(command, ct));

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateSupplyOrderStatusCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }
}

[ApiController, Route("api/wishlist"), Authorize]
public sealed class WishlistController(IMediator mediator) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<ProductDto>>> Get(CancellationToken ct) => Ok(await mediator.Send(new GetWishlistQuery(), ct));
    [HttpPost("{productId:int}")] public async Task<IActionResult> Add(int productId, CancellationToken ct) { await mediator.Send(new AddWishlistItemCommand(productId), ct); return NoContent(); }
    [HttpDelete("{productId:int}")] public async Task<IActionResult> Remove(int productId, CancellationToken ct) { await mediator.Send(new RemoveWishlistItemCommand(productId), ct); return NoContent(); }
}

[ApiController, Route("api/products/{productId:int}/reviews")]
public sealed class ProductReviewsController(IMediator mediator) : ControllerBase
{
    [HttpGet, AllowAnonymous] public async Task<ActionResult<IReadOnlyList<ProductReviewDto>>> Get(int productId, CancellationToken ct) => Ok(await mediator.Send(new ProductReviewsQuery(productId), ct));
    [HttpPost, Authorize] public async Task<ActionResult<ProductReviewDto>> Add(int productId, [FromBody] AddReviewBody body, CancellationToken ct) => Ok(await mediator.Send(new AddProductReviewCommand(productId, body.Rating, body.Text), ct));
}
public sealed record AddReviewBody(int Rating, string Text);
