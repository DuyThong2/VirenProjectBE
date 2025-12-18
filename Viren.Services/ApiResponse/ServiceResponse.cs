using System.Text.Json.Serialization;

namespace LexiMon.Service.ApiResponse;

public class ServiceResponse
{
    [JsonPropertyOrder(0)]
    public bool Succeeded { get; set; } = true;
    [JsonPropertyOrder(1)]
    public string Message { get; set; } = null!;
}