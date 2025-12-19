namespace Viren.Services.Dtos.Requests;

public class GetUsersPaginatedRequest
{
    public string? Search { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}