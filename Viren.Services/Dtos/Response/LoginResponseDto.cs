namespace Viren.Services.Dtos.Response;

public class LoginResponseDto
{
    public string Token { get; set; } = null!;
    public int ExpiredIn { get; set; } = 0;
}