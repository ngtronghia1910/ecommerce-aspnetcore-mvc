using System.ComponentModel.DataAnnotations;

namespace ECommerceWeb.Models.ViewModels;

public class ProfileViewModel
{
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập họ tên")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }
}
