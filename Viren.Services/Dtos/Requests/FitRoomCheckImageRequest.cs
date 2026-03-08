using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Viren.Services.Dtos.Requests;

public sealed class FitRoomCheckImageRequest
{
    [FromForm(Name = "image")]
    public IFormFile? Image { get; set; }
}
