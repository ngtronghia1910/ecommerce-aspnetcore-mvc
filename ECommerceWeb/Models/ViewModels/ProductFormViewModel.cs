using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ECommerceWeb.Models.ViewModels;

public class ProductFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(200)]
    [Display(Name = "Tên sản phẩm")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Required]
    [Range(0, 999999999)]
    [Display(Name = "Giá (VNĐ)")]
    public decimal Price { get; set; }

    [Range(0, 999999)]
    [Display(Name = "Tồn kho")]
    public int Stock { get; set; }

    [Display(Name = "Giảm giá (%)")]
    [Range(0, 100)]
    public decimal? DiscountPercent { get; set; }

    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    [Display(Name = "Ảnh sản phẩm")]
    public IFormFile? ImageFile { get; set; }

    public string? ExistingImageUrl { get; set; }
}
