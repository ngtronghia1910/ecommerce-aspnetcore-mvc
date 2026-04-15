using ECommerceWeb.Data;
using ECommerceWeb.Models;
using ECommerceWeb.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWeb.Controllers;

public class ProductsController : Controller
{
    private const int PageSize = 12;
    private readonly ApplicationDbContext _db;

    public ProductsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(
        string? q,
        int? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        string? sort,
        int page = 1)
    {
        if (page < 1) page = 1;

        var query = _db.Products
            .Include(p => p.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p => p.Name.Contains(term) ||
                                     (p.Description != null && p.Description.Contains(term)));
        }

        if (categoryId is > 0)
            query = query.Where(p => p.CategoryId == categoryId);

        if (minPrice is > 0)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice is > 0)
            query = query.Where(p => p.Price <= maxPrice.Value);

        sort = string.IsNullOrWhiteSpace(sort) ? "newest" : sort.ToLowerInvariant();
        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            _ => query.OrderByDescending(p => p.Id)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var vm = new ProductListViewModel
        {
            Paged = new PagedResult<Product>
            {
                Items = items,
                Page = page,
                PageSize = PageSize,
                TotalCount = total
            },
            Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync(),
            Query = q,
            CategoryId = categoryId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Sort = sort
        };

        vm.Pagination = new PaginationViewModel
        {
            CurrentPage = page,
            TotalPages = vm.Paged.TotalPages,
            Query = BuildQueryDict(q, categoryId, minPrice, maxPrice, sort)
        };

        return View(vm);
    }

    private static Dictionary<string, string?> BuildQueryDict(string? q, int? categoryId, decimal? minPrice, decimal? maxPrice, string sort)
    {
        var d = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(q)) d["q"] = q;
        if (categoryId is > 0) d["categoryId"] = categoryId.Value.ToString();
        if (minPrice is > 0) d["minPrice"] = minPrice.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (maxPrice is > 0) d["maxPrice"] = maxPrice.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(sort) && sort != "newest") d["sort"] = sort;
        return d;
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
            return NotFound();
        return View(product);
    }
}
