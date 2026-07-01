using ELearningPlatform.Core;

namespace ELearningPlatform.Application.DTOs.Coupons;

public class CreateCouponRequest
{
    public string Code { get; set; } = string.Empty;
    public DiscountTypeEnum DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? MaxUses { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? CourseId { get; set; }
}

public class ValidateCouponRequest
{
    public string Code { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public decimal OriginalPrice { get; set; }
}
