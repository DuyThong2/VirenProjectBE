namespace Viren.Services.Dtos.Response;

public sealed class MeshyTaskHistoryDto
{
    public Guid Id { get; set; }

    public string MeshyTaskId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int? Progress { get; set; }

    public string? ModelGlbUrl { get; set; }

    public string? ThumbnailUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}