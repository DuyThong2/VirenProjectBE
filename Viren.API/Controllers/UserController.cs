using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viren.Repositories.Utils;
using Viren.Services.Dtos.Requests;
using Viren.Services.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class UserController
{
    private readonly IUserService _userService;
    private readonly IUser _currentUser;


    public UserController(IUserService userService, IUser currentUser)
    {
        _userService = userService;
        _currentUser = currentUser;
        
    }

    [Authorize] 
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
    

    
    [Authorize(Policy = "AdminOrUser")]
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


    [Authorize(Policy = "AdminOrUser")]
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
    
    [Authorize(Policy = "AdminOnly")]
    [HttpGet]
    public async Task<IResult> GetUsersAsync(
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

        return TypedResults.Ok(result);
    }

}