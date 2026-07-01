using ELearningPlatform.Application;
using ELearningPlatform.Application.DTOs.Orders;
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
        return Ok(new GenericResponseDTO<object>(true, orders));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyOrders()
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var orders = await _orderService.GetUserOrdersAsync(userId);
        return Ok(new GenericResponseDTO<object>(true, orders));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        return order == null
            ? NotFound(new GenericResponseDTO<object>(false, "Order not found"))
            : !TryGetUserId(out var userId) ? Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"))
            : order.StudentId != userId && !User.IsInRole("Admin") ? StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"))
            : Ok(new GenericResponseDTO<object>(true, order));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequestDto request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        try
        {
            var order = await _orderService.CreateOrderAsync(userId, request.CourseIds);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, new GenericResponseDTO<object>(true, order));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new GenericResponseDTO<object>(false, ex.Message));
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Order not found"));
        }

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        if (order.StudentId != userId && !User.IsInRole("Admin"))
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

        var ok = await _orderService.CancelOrderAsync(id);
        if (!ok)
        {
            return BadRequest(new GenericResponseDTO<object>(false, "Order cannot be cancelled"));
        }

        return Ok(new GenericResponseDTO<object>(true, "Order cancelled"));
    }
}
