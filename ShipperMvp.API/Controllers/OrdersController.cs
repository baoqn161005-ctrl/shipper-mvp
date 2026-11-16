namespace ShipperMvp.API.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipperMvp.Application.Common.Exceptions;
using ShipperMvp.Application.Orders;
using ShipperMvp.Domain.Enums;

[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrdersController(IOrderService orders) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var created = await orders.CreateAsync(CurrentUserId(), request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("my-orders")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetMyOrders(CancellationToken ct) => Ok(await orders.GetCustomerOrdersAsync(CurrentUserId(), ct));

    [HttpPut("{id:int}/cancel")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct) { await orders.CancelAsync(id, CurrentUserId(), ct); return NoContent(); }

    [HttpGet("available")]
    [Authorize(Roles = "Shipper")]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetAvailable(CancellationToken ct) => Ok(await orders.GetAvailableAsync(ct));

    [HttpGet("shipper-orders")]
    [Authorize(Roles = "Shipper")]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetShipperOrders([FromQuery] bool history = false, CancellationToken ct = default) =>
        Ok(await orders.GetDriverOrdersAsync(CurrentUserId(), history, ct));

    [HttpPost("{id:int}/accept")]
    [Authorize(Roles = "Shipper")]
    public async Task<ActionResult<OrderResponse>> Accept(int id, CancellationToken ct) => Ok(await orders.AcceptAsync(id, CurrentUserId(), ct));

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Shipper")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(int id, UpdateOrderStatusRequest request, CancellationToken ct) =>
        Ok(await orders.UpdateStatusAsync(id, CurrentUserId(), request.NewStatus, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id, CancellationToken ct) =>
        Ok(await orders.GetByIdAsync(id, CurrentUserId(), User.IsInRole("Shipper"), ct));

    private int CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (int.TryParse(raw, out var id)) return id;
        throw new ForbiddenAccessException("Token không chứa user id hợp lệ.");
    }
}
