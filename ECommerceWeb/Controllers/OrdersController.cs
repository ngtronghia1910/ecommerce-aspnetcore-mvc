using ECommerceWeb.Data;
using ECommerceWeb.Models;
using ECommerceWeb.Models.ViewModels;
using ECommerceWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceWeb.Controllers;

[Authorize]
public class OrdersController : Controller
{
    public const string SessionCheckoutCoupon = "checkout_coupon_code";

    private readonly ApplicationDbContext _db;
    private readonly IVnpayService _vnpay;
    private readonly IMomoPaymentService _momo;
    private readonly ICouponService _couponService;

    public OrdersController(
        ApplicationDbContext db,
        IVnpayService vnpay,
        IMomoPaymentService momo,
        ICouponService couponService)
    {
        _db = db;
        _vnpay = vnpay;
        _momo = momo;
        _couponService = couponService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private static decimal CartSubtotal(IEnumerable<CartItem> cart) =>
        cart.Sum(c => ProductPricing.GetEffectiveUnitPrice(c.Product!) * c.Quantity);

    private async Task LoadCheckoutViewBagsAsync(List<CartItem> cart, CancellationToken ct)
    {
        var subtotal = CartSubtotal(cart);
        var code = HttpContext.Session.GetString(SessionCheckoutCoupon);

        ViewBag.VnpayReady = _vnpay.IsConfigured;
        ViewBag.MomoReady = _momo.IsConfigured;
        ViewBag.CartPreview = cart;
        ViewBag.Subtotal = subtotal;

        if (string.IsNullOrWhiteSpace(code))
        {
            ViewBag.CouponDiscount = 0m;
            ViewBag.GrandTotal = subtotal;
            ViewBag.AppliedCouponCode = null;
            ViewBag.CouponError = null;
            return;
        }

        var applied = await _couponService.TryApplyAsync(code, subtotal, DateTime.UtcNow, ct);
        var discount = applied.Success ? applied.DiscountAmount : 0m;
        ViewBag.CouponDiscount = discount;
        ViewBag.GrandTotal = subtotal - discount;
        ViewBag.AppliedCouponCode = applied.Success ? applied.Coupon!.Code : null;
        ViewBag.CouponError = applied.Success ? null : applied.ErrorMessage;
    }

    public async Task<IActionResult> MyOrders()
    {
        var orders = await _db.Orders
            .Include(o => o.OrderDetails)
            .Where(o => o.UserId == UserId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyCoupon(string? couponCode, CancellationToken ct)
    {
        var cart = await _db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == UserId)
            .ToListAsync(ct);

        if (!cart.Any())
        {
            TempData["Error"] = "Giỏ hàng trống.";
            return RedirectToAction("Index", "Cart");
        }

        var subtotal = CartSubtotal(cart);
        var result = await _couponService.TryApplyAsync(couponCode, subtotal, DateTime.UtcNow, ct);
        if (!result.Success)
        {
            HttpContext.Session.Remove(SessionCheckoutCoupon);
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            HttpContext.Session.SetString(SessionCheckoutCoupon, result.Coupon!.Code);
            TempData["Message"] = $"Đã áp dụng mã {result.Coupon.Code}. Giảm {result.DiscountAmount:N0} đ.";
        }

        return RedirectToAction(nameof(Checkout));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveCoupon()
    {
        HttpContext.Session.Remove(SessionCheckoutCoupon);
        TempData["Message"] = "Đã gỡ mã giảm giá.";
        return RedirectToAction(nameof(Checkout));
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(CancellationToken ct)
    {
        var cart = await _db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == UserId)
            .ToListAsync(ct);

        if (!cart.Any())
        {
            TempData["Error"] = "Giỏ hàng trống.";
            return RedirectToAction("Index", "Cart");
        }

        var user = await _db.Users.FirstAsync(u => u.Id == UserId, ct);
        await LoadCheckoutViewBagsAsync(cart, ct);

        var vm = new CheckoutViewModel
        {
            ShippingName = user.FullName,
            ShippingPhone = user.PhoneNumber ?? string.Empty,
            ShippingAddress = user.Address ?? string.Empty,
            PaymentKind = CheckoutPaymentKind.Cod,
            CouponCodeInput = HttpContext.Session.GetString(SessionCheckoutCoupon)
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model, CancellationToken ct)
    {
        var cart = await _db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == UserId)
            .ToListAsync(ct);

        if (!cart.Any())
        {
            TempData["Error"] = "Giỏ hàng trống.";
            return RedirectToAction("Index", "Cart");
        }

        foreach (var line in cart)
        {
            if (line.Product == null || line.Quantity > line.Product.Stock)
            {
                ModelState.AddModelError(string.Empty,
                    $"Sản phẩm \"{line.Product?.Name}\" không đủ tồn kho.");
            }
        }

        var subtotal = CartSubtotal(cart);
        var code = HttpContext.Session.GetString(SessionCheckoutCoupon);
        var couponApply = await _couponService.TryApplyAsync(code, subtotal, DateTime.UtcNow, ct);
        if (!string.IsNullOrWhiteSpace(code) && !couponApply.Success)
        {
            ModelState.AddModelError(string.Empty, couponApply.ErrorMessage ?? "Mã giảm giá không hợp lệ.");
        }

        var couponDiscount = couponApply.Success ? couponApply.DiscountAmount : 0m;
        var grandTotal = subtotal - couponDiscount;

        if (model.PaymentKind == CheckoutPaymentKind.VNPay && !_vnpay.IsConfigured)
            ModelState.AddModelError(nameof(model.PaymentKind), "VNPay chưa cấu hình (TmnCode, HashSecret).");
        if (model.PaymentKind == CheckoutPaymentKind.Momo && !_momo.IsConfigured)
            ModelState.AddModelError(nameof(model.PaymentKind), "MoMo chưa cấu hình (PartnerCode, AccessKey, SecretKey).");

        if (!ModelState.IsValid)
        {
            await LoadCheckoutViewBagsAsync(cart, ct);
            return View(model);
        }

        var isOnline = model.PaymentKind is CheckoutPaymentKind.VNPay or CheckoutPaymentKind.Momo;
        var paymentMethod = model.PaymentKind switch
        {
            CheckoutPaymentKind.VNPay => PaymentMethod.VNPay,
            CheckoutPaymentKind.Momo => PaymentMethod.Momo,
            _ => PaymentMethod.Cod
        };

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        Order order;
        try
        {
            order = new Order
            {
                UserId = UserId,
                OrderDate = DateTime.UtcNow,
                ShippingName = model.ShippingName,
                ShippingPhone = model.ShippingPhone,
                ShippingAddress = model.ShippingAddress,
                SubtotalAmount = subtotal,
                CouponDiscountAmount = couponDiscount,
                TotalAmount = grandTotal,
                CouponId = couponApply.Success ? couponApply.Coupon!.Id : null,
                AppliedCouponCode = couponApply.Success ? couponApply.Coupon!.Code : null,
                Status = isOnline ? OrderStatus.AwaitingPayment : OrderStatus.Pending,
                PaymentMethod = paymentMethod,
                PaymentStatus = isOnline ? PaymentStatus.Pending : PaymentStatus.None,
                StockDeducted = !isOnline,
                GatewayTxnRef = null
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync(ct);

            foreach (var line in cart)
            {
                var p = line.Product!;
                var unit = ProductPricing.GetEffectiveUnitPrice(p);
                _db.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = p.Id,
                    ProductName = p.Name,
                    UnitPrice = unit,
                    Quantity = line.Quantity
                });
                if (!isOnline)
                    p.Stock -= line.Quantity;
            }

            if (couponApply.Success)
            {
                var trackedCoupon = await _db.Coupons.FirstAsync(c => c.Id == couponApply.Coupon!.Id, ct);
                trackedCoupon.UsedCount++;
            }

            _db.CartItems.RemoveRange(cart);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        HttpContext.Session.Remove(SessionCheckoutCoupon);

        if (!isOnline)
        {
            TempData["Message"] = "Đặt hàng thành công (thanh toán khi nhận hàng). Cảm ơn bạn!";
            return RedirectToAction(nameof(MyOrders));
        }

        var txnRef = $"{order.Id}-{DateTime.UtcNow.Ticks}";
        order.GatewayTxnRef = txnRef;
        await _db.SaveChangesAsync(ct);

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        if (clientIp == "::1")
            clientIp = "127.0.0.1";

        if (model.PaymentKind == CheckoutPaymentKind.VNPay)
        {
            var returnUrl = Url.Action("VnpayReturn", "Payment", null, Request.Scheme, Request.Host.Value)!;
            var payUrl = _vnpay.BuildPaymentUrl(order.Id, order.TotalAmount, txnRef, clientIp, returnUrl);
            return Redirect(payUrl);
        }

        var momoReturn = Url.Action("MomoReturn", "Payment", null, Request.Scheme, Request.Host.Value)!;
        var momoNotify = Url.Action("MomoNotify", "Payment", null, Request.Scheme, Request.Host.Value)!;
        var momoRes = await _momo.CreatePaymentAsync(order.Id, order.TotalAmount, txnRef, momoReturn, momoNotify, ct);
        if (!momoRes.Success || string.IsNullOrEmpty(momoRes.PayUrl))
        {
            TempData["Error"] = $"Không tạo được link MoMo: {momoRes.Message} (mã {momoRes.ResultCode}).";
            return RedirectToAction(nameof(MyOrders));
        }

        return Redirect(momoRes.PayUrl);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == UserId);
        if (order == null)
            return NotFound();
        return View(order);
    }
}
