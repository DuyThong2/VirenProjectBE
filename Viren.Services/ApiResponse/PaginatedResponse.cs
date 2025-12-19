namespace Viren.Services.ApiResponse;

public class PaginatedResponse<T> : ServiceResponse
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages =>
        (int)Math.Ceiling(TotalItems / (double)PageSize);
    
    public List<T> Data { get; set; } = [];
}