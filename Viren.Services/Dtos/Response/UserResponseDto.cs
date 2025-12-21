
namespace Viren.Services.Dtos.Response;

public class UserResponseDto
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;
    public string? UserName { get; set; } // ✅ phục vụ login
    public string Role { get; set; } = null!;

    public string? Name { get; set; } // ✅ full name
    public string? PhoneNumber { get; set; }

    public string? FirstName { get; set; } = null;
    public string? LastName { get; set; } = null;
    public string? Address { get; set; } = null;

    public bool Gender { get; set; }
    public decimal? Height { get; set; }
    public decimal? Weight { get; set; }

    public int Status { get; set; } // hoặc CommonStatus nếu bạn muốn trả enum

    public string? AvatarImg { get; set; } = null;
    public DateTimeOffset? BirthDate { get; set; }

    public DateTime CreatedAt { get; set; }
}
