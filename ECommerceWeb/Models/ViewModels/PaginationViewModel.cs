namespace ECommerceWeb.Models.ViewModels;

public class PaginationViewModel
{
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public IDictionary<string, string?> Query { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;
}
