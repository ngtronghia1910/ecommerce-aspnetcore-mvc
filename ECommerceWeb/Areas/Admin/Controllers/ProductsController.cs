using ECommerceWeb.Data;
using ECommerceWeb.Models;
using ECommerceWeb.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class ProductsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProductsController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Products.Include(p => p.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim();
            query = query.Where(p => p.Name.Contains(t) ||
                                     (p.Description != null && p.Description.Contains(t)));
        }

        ViewBag.SearchQuery = q;
        return View(await query.OrderByDescending(p => p.Id).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        await LoadCategoriesAsync();
        return View(new ProductFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        await LoadCategoriesAsync();
        if (!ModelState.IsValid)
            return View(model);

        var imageUrl = await SaveImageAsync(model.ImageFile);
        _db.Products.Add(new Product
        {
            Name = model.Name,
            Description = model.Description,
            Price = model.Price,
            Stock = model.Stock,
            DiscountPercent = model.DiscountPercent is > 0 and <= 100 ? model.DiscountPercent : null,
            CategoryId = model.CategoryId,
            ImageUrl = imageUrl ?? "/images/placeholder-product.svg"
        });
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã thêm sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null)
            return NotFound();
        await LoadCategoriesAsync();
        return View(new ProductFormViewModel
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.Stock,
            DiscountPercent = p.DiscountPercent,
            CategoryId = p.CategoryId,
            ExistingImageUrl = p.ImageUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
    {
        if (id != model.Id)
            return NotFound();

        await LoadCategoriesAsync();
        if (!ModelState.IsValid)
            return View(model);

        var p = await _db.Products.FindAsync(id);
        if (p == null)
            return NotFound();

        p.Name = model.Name;
        p.Description = model.Description;
        p.Price = model.Price;
        p.Stock = model.Stock;
        p.DiscountPercent = model.DiscountPercent is > 0 and <= 100 ? model.DiscountPercent : null;
        p.CategoryId = model.CategoryId;

        var newUrl = await SaveImageAsync(model.ImageFile);
        if (!string.IsNullOrEmpty(newUrl))
            p.ImageUrl = newUrl;

        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã cập nhật sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null)
            return NotFound();
        _db.Products.Remove(p);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã xóa sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCategoriesAsync()
    {
        ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
    }

    private async Task<string?> SaveImageAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return null;

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || ext.Length > 10)
            ext = ".jpg";
        var safeName = $"{Guid.NewGuid():N}{ext}";
        var uploads = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploads);
        var path = Path.Combine(uploads, safeName);
        await using (var stream = System.IO.File.Create(path))
            await file.CopyToAsync(stream);
        return "/uploads/products/" + safeName;
    }
}
