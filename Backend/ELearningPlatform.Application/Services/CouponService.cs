using ELearningPlatform.Core;
using ELearningPlatform.Core.Interfaces;

namespace ELearningPlatform.Application.Services;

public class CouponService : ICouponService
{
    private readonly IUnitOfWork _unitOfWork;

    public CouponService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Coupon> CreateCouponAsync(string code, DiscountTypeEnum discountType, decimal discountValue,
        decimal? maxDiscountAmount, int? maxUses, DateTime? expiryDate, int instructorId, int? courseId)
    {
        var existing = await _unitOfWork.Coupons.FirstOrDefaultAsync(c => c.Code == code && !c.IsDeleted);
        if (existing != null)
            throw new InvalidOperationException($"A coupon with code '{code}' already exists.");

        var coupon = new Coupon
        {
            Code = code.ToUpperInvariant(),
            DiscountType = discountType,
            DiscountValue = discountValue,
            MaxDiscountAmount = maxDiscountAmount,
            MaxUses = maxUses,
            ExpiryDate = expiryDate,
            InstructorId = instructorId,
            CourseId = courseId,
            IsActive = true,
            UsedCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Coupons.AddAsync(coupon);
        await _unitOfWork.SaveChangesAsync();
        return coupon;
    }

    public async Task<IEnumerable<Coupon>> GetInstructorCouponsAsync(int instructorId)
    {
        return await _unitOfWork.Coupons.FindAsync(c => c.InstructorId == instructorId && !c.IsDeleted);
    }

    public async Task<Coupon?> GetCouponByCodeAsync(string code)
    {
        return await _unitOfWork.Coupons.FirstOrDefaultAsync(c => c.Code == code.ToUpperInvariant() && !c.IsDeleted);
    }

    public async Task<bool> DeactivateCouponAsync(int couponId, int instructorId)
    {
        var coupon = await _unitOfWork.Coupons.GetByIdAsync(couponId);
        if (coupon == null || coupon.InstructorId != instructorId || coupon.IsDeleted) return false;

        coupon.IsActive = false;
        coupon.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Coupons.Update(coupon);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCouponAsync(int couponId, int instructorId)
    {
        var coupon = await _unitOfWork.Coupons.GetByIdAsync(couponId);
        if (coupon == null || coupon.InstructorId != instructorId) return false;

        coupon.IsDeleted = true;
        coupon.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Coupons.Update(coupon);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<CouponValidationResult> ValidateCouponAsync(string code, int courseId, decimal originalPrice)
    {
        var coupon = await GetCouponByCodeAsync(code);

        if (coupon == null)
            return new CouponValidationResult(false, "Coupon not found.", 0, originalPrice);

        if (!coupon.IsActive)
            return new CouponValidationResult(false, "Coupon is no longer active.", 0, originalPrice);

        if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < DateTime.UtcNow)
            return new CouponValidationResult(false, "Coupon has expired.", 0, originalPrice);

        if (coupon.MaxUses.HasValue && coupon.UsedCount >= coupon.MaxUses.Value)
            return new CouponValidationResult(false, "Coupon usage limit has been reached.", 0, originalPrice);

        if (coupon.CourseId.HasValue && coupon.CourseId.Value != courseId)
            return new CouponValidationResult(false, "Coupon is not valid for this course.", 0, originalPrice);

        decimal discount = coupon.DiscountType == DiscountTypeEnum.Percentage
            ? originalPrice * (coupon.DiscountValue / 100m)
            : coupon.DiscountValue;

        if (coupon.MaxDiscountAmount.HasValue && discount > coupon.MaxDiscountAmount.Value)
            discount = coupon.MaxDiscountAmount.Value;

        if (discount > originalPrice) discount = originalPrice;

        return new CouponValidationResult(true, null, discount, originalPrice - discount);
    }
}
