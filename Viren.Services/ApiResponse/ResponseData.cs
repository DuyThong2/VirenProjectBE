using System.Text.Json.Serialization;

namespace Viren.Services.ApiResponse;

public class ResponseData<T> : ServiceResponse
{
    [JsonPropertyOrder(2)]
    public T? Data { get; set; }
}