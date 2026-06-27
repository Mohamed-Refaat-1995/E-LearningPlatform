using ELearningPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/coupons")]
[Authorize]
public class CouponController : ControllerBase
{
    private readonly ICouponService _couponService;

    public CouponController(ICouponService couponService)
    {
        _couponService = couponService;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirst("userId")?.Value, out userId);
    }

    [HttpGet]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> GetMyCoupons()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var coupons = await _couponService.GetInstructorCouponsAsync(userId);
        return Ok(coupons);
    }

    [HttpPost]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> Create([FromBody] CreateCouponRequest request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        try
        {
            var coupon = await _couponService.CreateCouponAsync(
                request.Code, request.DiscountType, request.DiscountValue,
                request.MaxDiscountAmount, request.MaxUses, request.ExpiryDate,
                userId, request.CourseId);

            return CreatedAtAction(nameof(GetMyCoupons), coupon);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/deactivate")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> Deactivate(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var ok = await _couponService.DeactivateCouponAsync(id, userId);
        if (!ok) return NotFound(new { message = "Coupon not found or not owned by you." });
        return Ok(new { message = "Coupon deactivated." });
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var ok = await _couponService.DeleteCouponAsync(id, userId);
        if (!ok) return NotFound(new { message = "Coupon not found or not owned by you." });
        return NoContent();
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidateCouponRequest request)
    {
        var result = await _couponService.ValidateCouponAsync(request.Code, request.CourseId, request.OriginalPrice);
        if (!result.IsValid)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new
        {
            discountAmount = result.DiscountAmount,
            finalPrice = result.FinalPrice
        });
    }
}

public record CreateCouponRequest(
    string Code,
    string DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    int? MaxUses,
    DateTime? ExpiryDate,
    int? CourseId);

public record ValidateCouponRequest(string Code, int CourseId, decimal OriginalPrice);
