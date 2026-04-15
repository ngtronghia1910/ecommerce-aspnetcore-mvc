using ECommerceWeb.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceWeb.Controllers;

public class ContactController : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new ContactViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(ContactViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        TempData["Message"] = "Cảm ơn bạn đã liên hệ. Chúng tôi sẽ phản hồi trong thời gian sớm nhất.";
        return RedirectToAction(nameof(Index));
    }
}
