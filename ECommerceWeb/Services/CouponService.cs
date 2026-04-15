using ECommerceWeb.Data;
using ECommerceWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWeb.Services;

public class CouponApplyResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Coupon? Coupon { get; set; }
    public decimal DiscountAmount { get; set; }
}

public interface ICouponService
{
    Task<CouponApplyResult> TryApplyAsync(string? code, decimal orderSubtotal, DateTime utcNow, CancellationToken ct = default);
}

public class CouponService : ICouponService
{
    private readonly ApplicationDbContext _db;

    public CouponService(ApplicationDbContext db) => _db = db;

    public async Task<CouponApplyResult> TryApplyAsync(string? code, decimal orderSubtotal, DateTime utcNow, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new CouponApplyResult { Success = false, ErrorMessage = "Nhập mã giảm giá." };

        var normalized = code.Trim().ToUpperInvariant();
        var coupon = await _db.Coupons
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == normalized, ct);

        if (coupon == null)
            return new CouponApplyResult { Success = false, ErrorMessage = "Mã không tồn tại." };

        if (!coupon.IsActive)
            return new CouponApplyResult { Success = false, ErrorMessage = "Mã không còn hiệu lực." };

        if (utcNow < coupon.StartDateUtc || utcNow > coupon.EndDateUtc)
            return new CouponApplyResult { Success = false, ErrorMessage = "Mã đã hết hạn hoặc chưa áp dụng." };

        if (coupon.UsageLimit is int lim && coupon.UsedCount >= lim)
            return new CouponApplyResult { Success = false, ErrorMessage = "Mã đã hết lượt sử dụng." };

        if (coupon.MinOrderValue is decimal min && orderSubtotal < min)
            return new CouponApplyResult
            {
                Success = false,
                ErrorMessage = $"Đơn tối thiểu {min:N0} đ để dùng mã này."
            };

        if (orderSubtotal <= 0)
            return new CouponApplyResult { Success = false, ErrorMessage = "Không thể áp dụng mã cho giỏ trống." };

        decimal discount;
        if (coupon.DiscountType == CouponDiscountType.Percent)
        {
            var pct = Math.Clamp(coupon.Value, 0, 100);
            discount = Math.Round(orderSubtotal * pct / 100, 0, MidpointRounding.AwayFromZero);
            if (coupon.MaxDiscountAmount is decimal cap && cap > 0)
                discount = Math.Min(discount, cap);
        }
        else
        {
            discount = Math.Min(Math.Round(coupon.Value, 0, MidpointRounding.AwayFromZero), orderSubtotal);
        }

        discount = Math.Min(discount, orderSubtotal);
        if (discount <= 0)
            return new CouponApplyResult { Success = false, ErrorMessage = "Không thể áp dụng mã cho đơn này." };

        return new CouponApplyResult
        {
            Success = true,
            Coupon = coupon,
            DiscountAmount = discount
        };
    }
}
