using ECommerceWeb.Data;
using ECommerceWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceWeb.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly ApplicationDbContext _db;

    public CartController(ApplicationDbContext db) => _db = db;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var items = await _db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == UserId)
            .ToListAsync();
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1, string? returnUrl = null)
    {
        if (quantity < 1) quantity = 1;

        var product = await _db.Products.FindAsync(productId);
        if (product == null)
            return NotFound();

        if (product.Stock < quantity)
        {
            TempData["Error"] = "Số lượng vượt quá tồn kho.";
            return SafeRedirect(returnUrl);
        }

        var existing = await _db.CartItems
            .FirstOrDefaultAsync(c => c.UserId == UserId && c.ProductId == productId);

        if (existing != null)
        {
            var newQty = existing.Quantity + quantity;
            if (newQty > product.Stock)
            {
                TempData["Error"] = "Số lượng trong giỏ vượt quá tồn kho.";
                return SafeRedirect(returnUrl);
            }
            existing.Quantity = newQty;
        }
        else
        {
            _db.CartItems.Add(new CartItem
            {
                UserId = UserId,
                ProductId = productId,
                Quantity = quantity
            });
        }

        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã thêm vào giỏ hàng.";
        return SafeRedirect(returnUrl);
    }

    private IActionResult SafeRedirect(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, int quantity)
    {
        var item = await _db.CartItems
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == UserId);
        if (item?.Product == null)
            return NotFound();

        if (quantity < 1)
            return await Remove(id);

        if (quantity > item.Product.Stock)
        {
            TempData["Error"] = "Số lượng vượt quá tồn kho.";
            return RedirectToAction(nameof(Index));
        }

        item.Quantity = quantity;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.UserId == UserId);
        if (item == null)
            return NotFound();
        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
