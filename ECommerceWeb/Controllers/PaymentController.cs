using System.Text.Json;
using ECommerceWeb.Data;
using ECommerceWeb.Models;
using ECommerceWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWeb.Controllers;

/// <summary>Callback từ VNPay / Momo (không yêu cầu đăng nhập; kiểm tra chữ ký + mã đơn).</summary>
[AllowAnonymous]
public class PaymentController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IVnpayService _vnpay;
    private readonly IMomoPaymentService _momo;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        ApplicationDbContext db,
        IVnpayService vnpay,
        IMomoPaymentService momo,
        ILogger<PaymentController> logger)
    {
        _db = db;
        _vnpay = vnpay;
        _momo = momo;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> VnpayReturn(CancellationToken ct) => HandleVnpayAsync(ct, isIpn: false);

    [HttpGet]
    public Task<IActionResult> VnpayIpn(CancellationToken ct) => HandleVnpayAsync(ct, isIpn: true);

    private async Task<IActionResult> HandleVnpayAsync(CancellationToken ct, bool isIpn)
    {
        var vnp = Request.Query
            .Where(q => q.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(q => q.Key, q => q.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        if (!vnp.TryGetValue("vnp_SecureHash", out var secureHash) || string.IsNullOrEmpty(secureHash))
            return isIpn ? VnpayIpnResponse(false) : RedirectToFailure("Thiếu chữ ký VNPay.");

        if (!_vnpay.ValidateSignature(vnp, secureHash))
            return isIpn ? VnpayIpnResponse(false) : RedirectToFailure("Chữ ký VNPay không hợp lệ.");

        var txn = _vnpay.GetTxnRef(vnp);
        if (string.IsNullOrEmpty(txn))
            return isIpn ? VnpayIpnResponse(false) : RedirectToFailure("Thiếu mã giao dịch.");

        var responseCode = _vnpay.GetResponseCode(vnp);
        vnp.TryGetValue("vnp_TransactionStatus", out var transStatus);

        var success = responseCode == "00" && transStatus == "00";

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var order = await _db.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.GatewayTxnRef == txn, ct);

            if (order == null)
            {
                await tx.RollbackAsync(ct);
                return isIpn ? VnpayIpnResponse(false) : RedirectToFailure("Không tìm thấy đơn hàng.");
            }

            if (success)
                await MarkOnlinePaidAsync(order, ct);
            else
                await MarkOnlineFailedAsync(order);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "VNPay callback error");
            return isIpn ? VnpayIpnResponse(false) : RedirectToFailure("Lỗi xử lý thanh toán.");
        }

        if (isIpn)
            return VnpayIpnResponse(true);

        if (success)
        {
            TempData["Message"] = "Thanh toán VNPay thành công. Cảm ơn bạn!";
            return RedirectToAction("MyOrders", "Orders");
        }

        TempData["Error"] = "Thanh toán VNPay không thành công hoặc đã hủy.";
        return RedirectToAction("MyOrders", "Orders");
    }

    private IActionResult VnpayIpnResponse(bool ok)
    {
        var json = ok
            ? """{"RspCode":"00","Message":"Confirm Success"}"""
            : """{"RspCode":"99","Message":"Unknown error"}""";
        return Content(json, "application/json");
    }

    [HttpGet]
    public async Task<IActionResult> MomoReturn(CancellationToken ct)
    {
        var q = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        if (!_momo.ValidateReturnSignature(q))
            return RedirectToFailure("Chữ ký MoMo không hợp lệ.");

        if (!q.TryGetValue("orderId", out var orderTxn) || string.IsNullOrEmpty(orderTxn))
            return RedirectToFailure("Thiếu orderId MoMo.");

        var ok = q.TryGetValue("resultCode", out var rcStr) && int.TryParse(rcStr, out var rc) && rc == 0;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var order = await _db.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.GatewayTxnRef == orderTxn, ct);

            if (order == null)
            {
                await tx.RollbackAsync(ct);
                return RedirectToFailure("Không tìm thấy đơn hàng.");
            }

            if (ok)
                await MarkOnlinePaidAsync(order, ct);
            else
                await MarkOnlineFailedAsync(order);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Momo return error");
            return RedirectToFailure("Lỗi xử lý thanh toán.");
        }

        if (ok)
        {
            TempData["Message"] = "Thanh toán MoMo thành công. Cảm ơn bạn!";
            return RedirectToAction("MyOrders", "Orders");
        }

        TempData["Error"] = "Thanh toán MoMo không thành công hoặc đã hủy.";
        return RedirectToAction("MyOrders", "Orders");
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MomoNotify(CancellationToken ct)
    {
        JsonDocument doc;
        try
        {
            doc = await JsonDocument.ParseAsync(Request.Body, cancellationToken: ct);
        }
        catch
        {
            return MomoNotifyResponse(false);
        }

        var root = doc.RootElement;
        if (!_momo.ValidateNotifySignature(root))
            return MomoNotifyResponse(false);

        if (!root.TryGetProperty("orderId", out var oidEl))
            return MomoNotifyResponse(false);
        var orderTxn = oidEl.GetString();
        if (string.IsNullOrEmpty(orderTxn))
            return MomoNotifyResponse(false);

        var ok = root.TryGetProperty("resultCode", out var rcEl) && rcEl.GetInt32() == 0;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var order = await _db.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.GatewayTxnRef == orderTxn, ct);

            if (order == null)
            {
                await tx.RollbackAsync(ct);
                return MomoNotifyResponse(false);
            }

            if (ok)
                await MarkOnlinePaidAsync(order, ct);
            else
                await MarkOnlineFailedAsync(order);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Momo notify error");
            return MomoNotifyResponse(false);
        }

        return MomoNotifyResponse(true);
    }

    private IActionResult MomoNotifyResponse(bool ok)
    {
        var payload = new { resultCode = ok ? 0 : 99, message = ok ? "Success" : "Failed" };
        return new JsonResult(payload);
    }

    private RedirectToActionResult RedirectToFailure(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction("MyOrders", "Orders");
    }

    private async Task MarkOnlinePaidAsync(Order order, CancellationToken ct)
    {
        if (order.PaymentStatus == PaymentStatus.Paid)
            return;

        if (!order.StockDeducted)
        {
            foreach (var d in order.OrderDetails)
            {
                var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == d.ProductId, ct);
                if (p == null)
                    continue;
                if (p.Stock < d.Quantity)
                    throw new InvalidOperationException($"Không đủ tồn kho cho sản phẩm #{d.ProductId}.");
                p.Stock -= d.Quantity;
            }

            order.StockDeducted = true;
        }

        order.PaymentStatus = PaymentStatus.Paid;
        order.Status = OrderStatus.Pending;
    }

    private Task MarkOnlineFailedAsync(Order order)
    {
        if (order.PaymentStatus == PaymentStatus.Paid)
            return Task.CompletedTask;

        order.PaymentStatus = PaymentStatus.Failed;
        order.Status = OrderStatus.Cancelled;
        return Task.CompletedTask;
    }
}
