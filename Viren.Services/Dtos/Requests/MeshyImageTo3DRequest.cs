namespace Viren.Services.Dtos.Requests;

public sealed class MeshyImageTo3DRequest
{
    public Guid FitRoomTaskId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string? ModelType { get; set; }

    public string? AiModel { get; set; }

    public string? Topology { get; set; }

    public int? TargetPolycount { get; set; }

    public bool? ShouldRemesh { get; set; }

    public bool? ShouldTexture { get; set; }

    public bool? EnablePbr { get; set; }

    public bool? RemoveLighting { get; set; }

    public bool? ImageEnhancement { get; set; }
}