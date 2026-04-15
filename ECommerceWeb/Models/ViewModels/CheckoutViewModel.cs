using System.ComponentModel.DataAnnotations;

namespace ECommerceWeb.Models.ViewModels;

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Nhập tên người nhận")]
    [Display(Name = "Họ tên người nhận")]
    public string ShippingName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập số điện thoại")]
    [StringLength(20, MinimumLength = 8, ErrorMessage = "Số điện thoại từ 8–20 ký tự")]
    [Display(Name = "Số điện thoại")]
    public string ShippingPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập địa chỉ giao hàng")]
    [Display(Name = "Địa chỉ giao hàng")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Display(Name = "Phương thức thanh toán")]
    public CheckoutPaymentKind PaymentKind { get; set; } = CheckoutPaymentKind.Cod;

    [Display(Name = "Mã giảm giá")]
    [StringLength(40)]
    public string? CouponCodeInput { get; set; }
}
