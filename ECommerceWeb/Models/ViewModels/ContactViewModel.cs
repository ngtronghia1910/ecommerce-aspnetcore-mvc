using System.ComponentModel.DataAnnotations;

namespace ECommerceWeb.Models.ViewModels;

public class ContactViewModel
{
    [Required(ErrorMessage = "Nhập họ tên")]
    [Display(Name = "Họ tên")]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập nội dung")]
    [StringLength(2000)]
    [Display(Name = "Nội dung")]
    public string Message { get; set; } = string.Empty;
}
