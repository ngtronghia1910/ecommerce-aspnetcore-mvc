namespace ECommerceWeb.Models;

public enum PaymentStatus
{
    /// <summary>Thanh toán khi nhận hàng — không qua cổng.</summary>
    None = 0,
    /// <summary>Đang chờ thanh toán online.</summary>
    Pending = 1,
    /// <summary>Đã thanh toán thành công qua cổng.</summary>
    Paid = 2,
    /// <summary>Thanh toán thất bại / bị hủy thanh toán.</summary>
    Failed = 3
}
