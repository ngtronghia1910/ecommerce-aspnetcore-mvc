using System.ComponentModel.DataAnnotations;

namespace ECommerceWeb.Models.ViewModels;

public enum CheckoutPaymentKind
{
    [Display(Name = "Thanh toán khi nhận hàng (COD)")]
    Cod = 0,
    [Display(Name = "VNPay (thẻ / QR sandbox)")]
    VNPay = 1,
    [Display(Name = "Ví MoMo (sandbox)")]
    Momo = 2
}
