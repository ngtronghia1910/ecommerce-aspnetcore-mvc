using ECommerceWeb.Models;

namespace ECommerceWeb.Models.ViewModels;

public class ProductListViewModel
{
    public PagedResult<Product> Paged { get; set; } = new();
    public PaginationViewModel Pagination { get; set; } = new();
    public IList<Category> Categories { get; set; } = new List<Category>();
    public string? Query { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string Sort { get; set; } = "newest";
}
