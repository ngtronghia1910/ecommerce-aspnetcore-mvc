using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceWeb.Models;

public class Order
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Required, StringLength(120)]
    public string ShippingName { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string ShippingPhone { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    /// <summary>Tổng sau giảm giá sản phẩm, trước mã coupon.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal SubtotalAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CouponDiscountAmount { get; set; }

    public int? CouponId { get; set; }
    public Coupon? Coupon { get; set; }

    [StringLength(40)]
    public string? AppliedCouponCode { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cod;

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.None;

    /// <summary>Mã giao dịch gửi lên cổng (vnp_TxnRef / orderId Momo) để đối chiếu callback.</summary>
    [StringLength(100)]
    public string? GatewayTxnRef { get; set; }

    /// <summary>Đã trừ tồn kho sau khi thanh toán thành công (tránh trừ trùng).</summary>
    public bool StockDeducted { get; set; }

    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
