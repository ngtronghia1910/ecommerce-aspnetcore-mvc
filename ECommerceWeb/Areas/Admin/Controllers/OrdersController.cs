using ECommerceWeb.Data;
using ECommerceWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _db;

    public OrdersController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim();
            query = query.Where(o =>
                (o.User != null && o.User.Email != null && o.User.Email.Contains(t)) ||
                o.Id.ToString() == t ||
                o.ShippingPhone.Contains(t));
        }

        ViewBag.SearchQuery = q;
        return View(await query.OrderByDescending(o => o.OrderDate).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
            return NotFound();
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null)
            return NotFound();
        order.Status = status;
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã cập nhật trạng thái đơn hàng.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
