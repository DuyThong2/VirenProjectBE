using Microsoft.AspNetCore.Http;

namespace Viren.Services.Dtos.Requests;

public sealed class UploadRequest
{
    public List<IFormFile>? Files { get; set; }
    public string? KeepJson { get; set; }
    public string? Meta { get; set; }
}