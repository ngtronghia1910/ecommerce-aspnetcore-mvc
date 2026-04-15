using ECommerceWeb.Data;
using ECommerceWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWeb.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ProductsApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProductsApiController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? q, [FromQuery] int? categoryId)
    {
        var query = _db.Products.AsNoTracking().Include(p => p.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p => p.Name.Contains(term));
        }
        if (categoryId is > 0)
            query = query.Where(p => p.CategoryId == categoryId);

        var raw = await query.OrderByDescending(p => p.Id).ToListAsync();
        var list = raw.Select(p => new
        {
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            OriginalPrice = p.Price,
            EffectivePrice = ProductPricing.GetEffectiveUnitPrice(p),
            p.DiscountPercent,
            p.Stock,
            p.ImageUrl,
            Category = p.Category!.Name
        });
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _db.Products.AsNoTracking()
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p == null)
            return NotFound();
        return Ok(new
        {
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            OriginalPrice = p.Price,
            EffectivePrice = ProductPricing.GetEffectiveUnitPrice(p),
            p.DiscountPercent,
            p.Stock,
            p.ImageUrl,
            Category = p.Category?.Name,
            p.CategoryId
        });
    }
}
