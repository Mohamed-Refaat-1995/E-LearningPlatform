using ELearningPlatform.Application.DTOs.Orders;
using ELearningPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirst("userId")?.Value, out userId);
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _orderService.GetAllOrdersAsync();
        return Ok(orders);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyOrders()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var orders = await _orderService.GetUserOrdersAsync(userId);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound(new { message = "Order not found" });

        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (order.UserId != userId && !User.IsInRole("Admin"))
            return Forbid();

        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequestDto request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        try
        {
            var order = await _orderService.CreateOrderAsync(userId, request.CourseIds);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound(new { message = "Order not found" });

        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (order.UserId != userId && !User.IsInRole("Admin"))
            return Forbid();

        var ok = await _orderService.CancelOrderAsync(id);
        if (!ok) return BadRequest(new { message = "Order cannot be cancelled" });
        return Ok(new { message = "Order cancelled" });
    }
}
