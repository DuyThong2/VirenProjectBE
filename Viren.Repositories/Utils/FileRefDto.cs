namespace Viren.Services.Dtos.Requests;

public sealed class FileRefDto
{
    public string? Key { get; set; }
    public string? Url { get; set; }              // stable view url (strip query)
    public string? Name { get; set; }             // display name
    public long? Size { get; set; }               // bytes
    public string? ContentType { get; set; }      // e.g. image/png
    public DateTime? UploadedAtUtc { get; set; }  // ISO8601
}