using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Viren.Services.Dtos.Requests;

public sealed class FitRoomTryOnRequest
{
    [FromForm(Name = "model_image")]
    public IFormFile? ModelImage { get; set; }

    [FromForm(Name = "cloth_image")]
    public IFormFile? ClothImage { get; set; }

    [FromForm(Name = "lower_cloth_image")]
    public IFormFile? LowerClothImage { get; set; }

    [FromForm(Name = "cloth_type")]
    public string? ClothType { get; set; } = "upper";

    [FromForm(Name = "hd_mode")]
    public string? HdMode { get; set; } = "true";
}
