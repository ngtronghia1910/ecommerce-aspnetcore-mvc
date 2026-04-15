using System.Security.Claims;
using ECommerceWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWeb.ViewComponents;

public class CartCountViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;

    public CartCountViewComponent(ApplicationDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
            return View(0);

        var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return View(0);

        var qty = await _db.CartItems
            .Where(c => c.UserId == userId)
            .SumAsync(c => (int?)c.Quantity) ?? 0;
        return View(qty);
    }
}
