using ECommerceWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWeb.ViewComponents;

public class CategoryMenuViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;

    public CategoryMenuViewComponent(ApplicationDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var items = await _db.Categories.OrderBy(c => c.Name).Take(24).ToListAsync();
        return View(items);
    }
}
