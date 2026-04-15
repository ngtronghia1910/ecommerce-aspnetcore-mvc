using ECommerceWeb.Models;

namespace ECommerceWeb.Models.ViewModels;

public class HomePageViewModel
{
    public IList<Category> Categories { get; set; } = new List<Category>();
    public IList<Product> FeaturedProducts { get; set; } = new List<Product>();
    public IList<Product> NewProducts { get; set; } = new List<Product>();
}
