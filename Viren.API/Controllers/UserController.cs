
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Viren.Repositories.Storage.Bucket;
using Viren.Repositories.Utils;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;
using Viren.Services.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IS3Storage _storage;

    private readonly IUser _currentUser;


    public UserController(IUserService userService, IUser currentUser, IS3Storage storage)
    {
        _userService = userService;
        _currentUser = currentUser;
        _storage = storage;

    }

    /*
    [Authorize] 
    */
    [HttpGet("whoami")]
    public IResult WhoAmI()
    {
        return TypedResults.Ok(new
        {
            Id = _currentUser.Id,
            Email = _currentUser.Email,
            Roles = _currentUser.Roles
        });
    }
    

    
    /*
    [Authorize(Policy = "AdminOrUser")]
    */
    [HttpGet("profile")]
    public async Task<IResult> GetProfileAsync(
        [FromQuery] Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var serviceResponse = await _userService.GetUserByIdAsync(
            userId,
            cancellationToken
        );

        return serviceResponse.Succeeded
            ? TypedResults.Ok(serviceResponse)
            : TypedResults.Unauthorized();
    }


    /*[Authorize(Policy = "AdminOrUser")]*/
    [HttpPut("profile")]
    public async Task<IResult> UpdateProfileAsync(
        [FromQuery] Guid? userId,
        [FromBody] UserRequestDto requestBody,
        CancellationToken cancellationToken = default)
    {
        var serviceResponse = await _userService.UpdateAsync(
            userId,
            requestBody,
            cancellationToken
        );

        return serviceResponse.Succeeded
            ? TypedResults.Ok(serviceResponse)
            : TypedResults.BadRequest(serviceResponse);
    }
    
    /*
    [Authorize(Policy = "AdminOnly")]
    */
    [HttpGet]
    public async Task<IActionResult> GetUsersAsync(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;
        if (pageSize > 100) pageSize = 100; 

        var request = new GetUsersPaginatedRequest
        {
            Search = search,
            Page = page,
            PageSize = pageSize
        };

        var result = await _userService.GetUsersAsync(request, cancellationToken);

        return Ok(result);
    }
    
    [HttpPost("{userId:guid}/files")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    [ProducesResponseType(typeof(ReconcileResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Reconcile(
        [FromRoute] Guid userId,
        [FromForm] UploadRequest req,
        CancellationToken ct)
    {
        var resp = await _userService.ReconcileUserFilesAsync(userId, req.KeepJson, req.Files, req.Meta, ct);

        Console.WriteLine($"[User Reconcile] user={userId} uploaded={resp.UploadedFiles.Count} desired={resp.Desired.Count}");

        return Created(String.Empty, resp);
    }
    
    [HttpGet("download/{**key}")]
    public async Task<IActionResult> Download([FromRoute] string key, [FromQuery] string? filename)
    {
        var file = await _storage.DownloadAsync(key);
        if (!string.IsNullOrWhiteSpace(filename))
            file.FileDownloadName = filename;
        return file; 
    }

    [HttpPost]
    public async Task<IActionResult> CreateUserAsync(
        [FromBody] UserCreateRequestDto requestBody,
        CancellationToken ct = default)
    {
        var res = await _userService.CreateAsync(requestBody, ct);
        return res.Succeeded ? Created(String.Empty, res) :BadRequest(res);
    }

}