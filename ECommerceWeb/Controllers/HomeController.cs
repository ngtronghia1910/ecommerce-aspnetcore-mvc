using System.Diagnostics;
using ECommerceWeb.Data;
using ECommerceWeb.Models;
using ECommerceWeb.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWeb.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ApplicationDbContext db, ILogger<HomeController> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _db.Categories.OrderBy(c => c.Name).Take(8).ToListAsync();
        var featured = await _db.Products
            .Include(p => p.Category)
            .OrderByDescending(p => p.Id)
            .Take(8)
            .ToListAsync();
        var newest = await _db.Products
            .Include(p => p.Category)
            .OrderByDescending(p => p.Id)
            .Take(4)
            .ToListAsync();

        var vm = new HomePageViewModel
        {
            Categories = categories,
            FeaturedProducts = featured,
            NewProducts = newest
        };
        return View(vm);
    }

    public async Task<IActionResult> Categories()
    {
        var list = await _db.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();
        return View(list);
    }

    public IActionResult Privacy() => View();

    public IActionResult ReturnPolicy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
