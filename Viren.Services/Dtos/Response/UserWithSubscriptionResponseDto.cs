namespace Viren.Services.Dtos.Response;

public class UserWithSubscriptionResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;

    public string?  LastName { get; set; } 
    public string? FirstName { get; set; } 
    public string? PhoneNumber { get; set; }
    public List<string> ? SubscriptionName { get; set; }
    
    public string ? ImgUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public string ? status { get; set; }
}