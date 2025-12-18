
namespace Viren.Services.Dtos.Response;

public class UserResponseDto
{
    public Guid Id { get; set; } 
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string? FirstName { get; set; } = null;
    public string? LastName { get; set; } = null;
    public string? Address { get; set; } = null;
    public DateTimeOffset? BirthDate { get; set; }
}