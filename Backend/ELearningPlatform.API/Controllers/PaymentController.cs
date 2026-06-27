using ELearningPlatform.Application.DTOs.Orders;
using ELearningPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IOrderService _orderService;
    private readonly IEnrollmentService _enrollmentService;

    public PaymentController(
        IPaymentService paymentService,
        IOrderService orderService,
        IEnrollmentService enrollmentService)
    {
        _paymentService = paymentService;
        _orderService = orderService;
        _enrollmentService = enrollmentService;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirst("userId")?.Value, out userId);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyPayments()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var payments = await _paymentService.GetUserPaymentsAsync(userId);
        return Ok(payments);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyPaymentsAlias()
    {
        return await GetMyPayments();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var payment = await _paymentService.GetPaymentByIdAsync(id);
        if (payment == null) return NotFound(new { message = "Payment not found" });
        return Ok(payment);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequestDto request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var order = await _orderService.GetOrderByIdAsync(request.OrderId);
        if (order == null) return NotFound(new { message = "Order not found" });
        if (order.UserId != userId) return Forbid();

        var payment = await _paymentService.CreatePaymentAsync(request.OrderId, request.Amount, request.PaymentMethod);
        return Ok(payment);
    }

    [HttpPost("{id}/process")]
    public async Task<IActionResult> Process(int id, [FromBody] ProcessPaymentRequestDto request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var payment = await _paymentService.GetPaymentByIdAsync(id);
        if (payment == null) return NotFound(new { message = "Payment not found" });

        var order = await _orderService.GetOrderByIdAsync(payment.OrderId);
        if (order == null) return NotFound(new { message = "Order not found" });
        if (order.UserId != userId) return Forbid();

        var ok = await _paymentService.ProcessPaymentAsync(id, request.GetTransactionNo());
        if (!ok) return BadRequest(new { message = "Payment processing failed" });

        foreach (var item in order.Items)
        {
            if (!await _enrollmentService.IsEnrolledAsync(order.UserId, item.CourseId))
                await _enrollmentService.EnrollStudentAsync(order.UserId, item.CourseId, item.Price);
        }

        var invoice = await _paymentService.GenerateInvoiceAsync(id);
        return Ok(new { payment, invoice });
    }

    [HttpPost("{id}/refund")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Refund(int id)
    {
        var ok = await _paymentService.RefundPaymentAsync(id);
        if (!ok) return BadRequest(new { message = "Refund failed" });
        return Ok(new { message = "Payment refunded" });
    }
}
