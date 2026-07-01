namespace ELearningPlatform.Core.Interfaces;

public interface ICouponService
{
    Task<Coupon> CreateCouponAsync(string code, DiscountTypeEnum discountType, decimal discountValue,
        decimal? maxDiscountAmount, int? maxUses, DateTime? expiryDate, int instructorId, int? courseId);
    Task<IEnumerable<Coupon>> GetInstructorCouponsAsync(int instructorId);
    Task<Coupon?> GetCouponByCodeAsync(string code);
    Task<bool> DeactivateCouponAsync(int couponId, int instructorId);
    Task<bool> DeleteCouponAsync(int couponId, int instructorId);
    Task<CouponValidationResult> ValidateCouponAsync(string code, int courseId, decimal originalPrice);
}

public record CouponValidationResult(bool IsValid, string? ErrorMessage, decimal DiscountAmount, decimal FinalPrice);
