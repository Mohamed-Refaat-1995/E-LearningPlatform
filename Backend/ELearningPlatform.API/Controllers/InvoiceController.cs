using ELearningPlatform.Application;
using ELearningPlatform.Application.DTOs.Invoices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IOrderService _orderService;
    private readonly IUnitOfWork _unitOfWork;

    public InvoicesController(
        IPaymentService paymentService,
        IOrderService orderService,
        IUnitOfWork unitOfWork)
    {
        _paymentService = paymentService;
        _orderService = orderService;
        _unitOfWork = unitOfWork;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirst("userId")?.Value, out userId);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyInvoices()
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var payments = (await _paymentService.GetUserPaymentsAsync(userId)).ToList();
        var paymentIds = payments.Select(p => p.Id).ToHashSet();

        var invoices = await _unitOfWork.Invoices.FindAsync(i =>
            paymentIds.Contains(i.PaymentId) && !i.IsDeleted
        );

        return Ok(new GenericResponseDTO<object>(true, invoices));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var invoice = await _unitOfWork.Invoices.GetByIdAsync(id);
        if (invoice == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Invoice not found"));
        }

        var payment = await _paymentService.GetPaymentByIdAsync(invoice.PaymentId);
        if (payment == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Payment not found"));
        }

        var order = await _orderService.GetOrderByIdAsync(payment.OrderId);
        if (order == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Order not found"));
        }

        //if (order.UserId != userId && !User.IsInRole("Admin"))
        //    return Forbid();

        return Ok(new GenericResponseDTO<object>(true, invoice));
    }

    [HttpPost]
    public async Task<IActionResult> Generate([FromBody] GenerateInvoiceRequestDto request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var payment = await _paymentService.GetPaymentByIdAsync(request.PaymentId);
        if (payment == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Payment not found"));
        }

        var order = await _orderService.GetOrderByIdAsync(payment.OrderId);
        if (order == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Order not found"));
        }

        if (order.StudentId != userId && !User.IsInRole("Admin"))
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

        var invoice = await _paymentService.GenerateInvoiceAsync(request.PaymentId);
        return Ok(new GenericResponseDTO<object>(true, invoice));
    }
}
