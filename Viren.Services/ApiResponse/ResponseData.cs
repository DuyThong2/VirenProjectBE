using System.Text.Json.Serialization;

namespace LexiMon.Service.ApiResponse;

public class ResponseData<T> : ServiceResponse
{
    [JsonPropertyOrder(2)]
    public T? Data { get; set; }
}