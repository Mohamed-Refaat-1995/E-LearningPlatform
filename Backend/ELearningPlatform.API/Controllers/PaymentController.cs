using ELearningPlatform.Application;
using ELearningPlatform.Application.DTOs.Orders;
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
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var payments = await _paymentService.GetUserPaymentsAsync(userId);
        return Ok(new GenericResponseDTO<object>(true, payments));
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
        return payment == null
            ? NotFound(new GenericResponseDTO<object>(false, "Payment not found"))
            : Ok(new GenericResponseDTO<object>(true, payment));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequestDto request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var order = await _orderService.GetOrderByIdAsync(request.OrderId);
        if (order == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Order not found"));
        }

        if (order.StudentId != userId)
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

        var result = await _paymentService.CreatePaymentAsync(request.OrderId, request.Amount, request.PaymentMethod);
        return Ok(new GenericResponseDTO<object>(true, new { payment = result.Payment, clientSecret = result.ClientSecret }));
    }

    [HttpPost("{id}/process")]
    public async Task<IActionResult> Process(int id, [FromBody] ProcessPaymentRequestDto request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var payment = await _paymentService.GetPaymentByIdAsync(id);
        if (payment == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Payment not found"));
        }

        var order = await _orderService.GetOrderByIdAsync(payment.OrderId);
        if (order == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Order not found"));
        }

        if (order.StudentId != userId)
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

        var (ok, error) = await _paymentService.ProcessPaymentAsync(id, request.GetTransactionNo());
        if (!ok)
        {
            return BadRequest(new GenericResponseDTO<object>(false, error ?? "Payment processing failed"));
        }

        foreach (var item in order.Items)
        {
            if (!await _enrollmentService.IsEnrolledAsync(order.StudentId, item.CourseId))
            {
                await _enrollmentService.EnrollStudentAsync(order.StudentId, item.CourseId, item.Price);
            }
        }

        var invoice = await _paymentService.GenerateInvoiceAsync(id);
        return Ok(new GenericResponseDTO<object>(true, new { payment, invoice }));
    }

    [HttpPost("{id}/refund")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Refund(int id)
    {
        var ok = await _paymentService.RefundPaymentAsync(id);
        if (!ok)
        {
            return BadRequest(new GenericResponseDTO<object>(false, "Refund failed"));
        }

        return Ok(new GenericResponseDTO<object>(true, "Payment refunded"));
    }
}
