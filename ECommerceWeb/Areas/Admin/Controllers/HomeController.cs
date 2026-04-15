using ECommerceWeb.Data;
using ECommerceWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.ProductCount = await _db.Products.CountAsync();
        ViewBag.OrderCount = await _db.Orders.CountAsync();
        ViewBag.PendingOrders = await _db.Orders.CountAsync(o =>
            o.Status == OrderStatus.Pending || o.Status == OrderStatus.AwaitingPayment);
        ViewBag.UserCount = await _db.Users.CountAsync();

        var revenue = await _db.Orders
            .Where(o => o.Status != OrderStatus.Cancelled &&
                        (o.PaymentStatus == PaymentStatus.Paid ||
                         (o.PaymentMethod == PaymentMethod.Cod && o.PaymentStatus == PaymentStatus.None)))
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        ViewBag.Revenue = revenue;
        return View();
    }
}
