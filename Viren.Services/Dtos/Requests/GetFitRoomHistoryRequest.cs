namespace Viren.Services.Dtos.Requests;

public sealed class GetFitRoomHistoryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
