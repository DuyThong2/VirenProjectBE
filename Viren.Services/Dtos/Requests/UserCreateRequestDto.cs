
namespace Viren.Services.Dtos.Requests;

public class UserCreateRequestDto : UserRequestDto
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;

    public string? UserName { get; set; }         
    public string Role { get; set; } = "User";     
    public string? AuthProvider { get; set; }      
}