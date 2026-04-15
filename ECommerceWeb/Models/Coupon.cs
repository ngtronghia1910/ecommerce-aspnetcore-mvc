using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceWeb.Models;

public class Coupon
{
    public int Id { get; set; }

    [Required, StringLength(40)]
    public string Code { get; set; } = string.Empty;

    public CouponDiscountType DiscountType { get; set; }

    /// <summary>Phần trăm (0–100) hoặc số tiền VNĐ cố định.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Value { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinOrderValue { get; set; }

    /// <summary>Giới hạn tối đa tiền giảm khi loại %.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaxDiscountAmount { get; set; }

    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }

    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }

    public bool IsActive { get; set; } = true;
}
