namespace Viren.Services.Dtos.Response;

public sealed class FitRoomProxyResponse
{
    public int StatusCode { get; init; }
    public string Content { get; init; } = "{}";
    public string ContentType { get; init; } = "application/json";
}
