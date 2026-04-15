using ECommerceWeb.Data;
using ECommerceWeb.Models;
using ECommerceWeb.Models.ViewModels;
using ECommerceWeb.Time;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class CouponsController : Controller
{
    private readonly ApplicationDbContext _db;

    public CouponsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var list = await _db.Coupons.AsNoTracking()
            .OrderByDescending(c => c.Id)
            .ToListAsync();
        return View(list);
    }

    public IActionResult Create()
    {
        var now = VietnamTime.UtcToLocal(DateTime.UtcNow);
        return View(new CouponFormViewModel
        {
            StartDateLocal = now.Date,
            EndDateLocal = now.Date.AddMonths(1).AddHours(23).AddMinutes(59),
            DiscountType = CouponDiscountType.Percent,
            Value = 10
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CouponFormViewModel model)
    {
        Normalize(model);
        if (model.EndDateLocal <= model.StartDateLocal)
            ModelState.AddModelError(nameof(model.EndDateLocal), "Thời gian kết thúc phải sau thời gian bắt đầu.");

        if (!ModelState.IsValid)
            return View(model);

        var code = model.Code.Trim().ToUpperInvariant();
        if (await _db.Coupons.AnyAsync(c => c.Code == code))
        {
            ModelState.AddModelError(nameof(model.Code), "Mã này đã tồn tại.");
            return View(model);
        }

        _db.Coupons.Add(new Coupon
        {
            Code = code,
            DiscountType = model.DiscountType,
            Value = model.Value,
            MinOrderValue = model.MinOrderValue > 0 ? model.MinOrderValue : null,
            MaxDiscountAmount = model.MaxDiscountAmount > 0 ? model.MaxDiscountAmount : null,
            StartDateUtc = VietnamTime.LocalToUtc(model.StartDateLocal),
            EndDateUtc = VietnamTime.LocalToUtc(model.EndDateLocal),
            UsageLimit = model.UsageLimit,
            UsedCount = 0,
            IsActive = model.IsActive
        });
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã tạo mã giảm giá.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var c = await _db.Coupons.FindAsync(id);
        if (c == null)
            return NotFound();

        return View(new CouponFormViewModel
        {
            Id = c.Id,
            Code = c.Code,
            DiscountType = c.DiscountType,
            Value = c.Value,
            MinOrderValue = c.MinOrderValue,
            MaxDiscountAmount = c.MaxDiscountAmount,
            StartDateLocal = VietnamTime.UtcToLocal(c.StartDateUtc),
            EndDateLocal = VietnamTime.UtcToLocal(c.EndDateUtc),
            UsageLimit = c.UsageLimit,
            IsActive = c.IsActive,
            UsedCount = c.UsedCount
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CouponFormViewModel model)
    {
        if (id != model.Id)
            return NotFound();

        Normalize(model);
        if (model.EndDateLocal <= model.StartDateLocal)
            ModelState.AddModelError(nameof(model.EndDateLocal), "Thời gian kết thúc phải sau thời gian bắt đầu.");

        if (!ModelState.IsValid)
            return View(model);

        var c = await _db.Coupons.FindAsync(id);
        if (c == null)
            return NotFound();

        var code = model.Code.Trim().ToUpperInvariant();
        if (await _db.Coupons.AnyAsync(x => x.Code == code && x.Id != id))
        {
            ModelState.AddModelError(nameof(model.Code), "Mã này đã tồn tại.");
            return View(model);
        }

        c.Code = code;
        c.DiscountType = model.DiscountType;
        c.Value = model.Value;
        c.MinOrderValue = model.MinOrderValue > 0 ? model.MinOrderValue : null;
        c.MaxDiscountAmount = model.MaxDiscountAmount > 0 ? model.MaxDiscountAmount : null;
        c.StartDateUtc = VietnamTime.LocalToUtc(model.StartDateLocal);
        c.EndDateUtc = VietnamTime.LocalToUtc(model.EndDateLocal);
        c.UsageLimit = model.UsageLimit;
        c.IsActive = model.IsActive;

        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã cập nhật mã giảm giá.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Coupons.FindAsync(id);
        if (c == null)
            return NotFound();

        var usedOnOrder = await _db.Orders.AnyAsync(o => o.CouponId == id);
        if (usedOnOrder)
        {
            TempData["Error"] = "Không xóa được: mã đã gắn với đơn hàng. Có thể tắt kích hoạt thay vì xóa.";
            return RedirectToAction(nameof(Index));
        }

        _db.Coupons.Remove(c);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã xóa mã giảm giá.";
        return RedirectToAction(nameof(Index));
    }

    private static void Normalize(CouponFormViewModel model)
    {
        model.Code = model.Code.Trim();
    }
}
