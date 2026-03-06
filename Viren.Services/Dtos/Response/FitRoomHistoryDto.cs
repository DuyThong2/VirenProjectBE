namespace Viren.Services.Dtos.Response;

public sealed class FitRoomHistoryDto
{
    public Guid Id { get; set; }
    public string TaskId { get; set; } = string.Empty;
    public string ClothType { get; set; } = string.Empty;
    public bool HdMode { get; set; }
    public string? ModelImageUrl { get; set; }
    public string? ClothImageUrl { get; set; }
    public string? LowerClothImageUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? Progress { get; set; }
    public string? ResultUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}
