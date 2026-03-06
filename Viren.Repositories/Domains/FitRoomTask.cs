using Viren.Repositories.Common;

namespace Viren.Repositories.Domains;

public sealed class FitRoomTask : BaseEntity<Guid>
{
    public string FitRoomTaskId { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string ClothType { get; set; } = string.Empty;
    public bool HdMode { get; set; }
    public string ModelImageKey { get; set; } = string.Empty;
    public string ModelImageUrl { get; set; } = string.Empty;
    public string ClothImageKey { get; set; } = string.Empty;
    public string ClothImageUrl { get; set; } = string.Empty;
    public string? LowerClothImageKey { get; set; }
    public string? LowerClothImageUrl { get; set; }
    public string Status { get; set; } = "CREATED";
    public int? Progress { get; set; }
    public string? ResultUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public string LatestResponseJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}
