namespace ECommerceWeb.Models;

public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4,
    /// <summary>Đơn đã tạo, chờ khách hoàn tất thanh toán online.</summary>
    AwaitingPayment = 5
}
