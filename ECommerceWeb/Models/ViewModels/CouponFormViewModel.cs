using System.ComponentModel.DataAnnotations;

namespace ECommerceWeb.Models.ViewModels;

public class CouponFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(40, MinimumLength = 1)]
    [Display(Name = "Mã (code)")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Loại giảm")]
    public CouponDiscountType DiscountType { get; set; }

    [Display(Name = "Giá trị (% hoặc VNĐ)")]
    [Range(0.01, 999999999)]
    public decimal Value { get; set; }

    [Display(Name = "Đơn tối thiểu (VNĐ)")]
    [Range(0, 999999999)]
    public decimal? MinOrderValue { get; set; }

    [Display(Name = "Giảm tối đa khi loại % (VNĐ)")]
    [Range(0, 999999999)]
    public decimal? MaxDiscountAmount { get; set; }

    [Display(Name = "Bắt đầu (giờ VN)")]
    public DateTime StartDateLocal { get; set; }

    [Display(Name = "Kết thúc (giờ VN)")]
    public DateTime EndDateLocal { get; set; }

    [Display(Name = "Giới hạn lượt dùng (để trống = không giới hạn)")]
    [Range(1, int.MaxValue)]
    public int? UsageLimit { get; set; }

    [Display(Name = "Đang kích hoạt")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Đã dùng")]
    public int UsedCount { get; set; }
}
