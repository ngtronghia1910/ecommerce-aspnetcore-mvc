using System.ComponentModel.DataAnnotations;

namespace ECommerceWeb.Models;

public class CartItem
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Range(1, 9999)]
    public int Quantity { get; set; } = 1;
}
