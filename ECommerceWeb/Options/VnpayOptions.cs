namespace ECommerceWeb.Options;

public class VnpayOptions
{
    public const string SectionName = "VNPay";

    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
    /// <summary>URL tuyệt đối (public) VNPay redirect sau thanh toán, ví dụ https://yourdomain.com/Payment/VnpayReturn</summary>
    public string ReturnUrl { get; set; } = string.Empty;
    /// <summary>URL IPN (VNPay gọi server). Localhost cần tunnel (ngrok) để nhận IPN.</summary>
    public string? IpnUrl { get; set; }
    public string PaymentUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
}
