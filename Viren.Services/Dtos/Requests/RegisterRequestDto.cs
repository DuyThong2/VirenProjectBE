using System.ComponentModel.DataAnnotations;

namespace Viren.Services.Dtos.Requests;

public class RegisterRequestDto
{
    [EmailAddress(ErrorMessage = "Vui lòng nhập email hợp lệ!")]
    [Required(ErrorMessage = "Vui lòng nhập email!")]
    public string Email { get; set; } = null!;
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu!")]
    public string Password { get; set; } = null!;
    public string? FirstName { get; set; } = null!;
    public string? LastName { get; set; } = null!;
    
}