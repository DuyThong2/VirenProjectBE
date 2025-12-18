
namespace Viren.Services.Dtos.Requests;

public class UserRequestDto
{
    public string? FirstName { get; set; } = null;
    public string? LastName { get; set; } = null;
    public string? Address { get; set; } = null;
    public DateTime? BirthDate { get; set; }
}