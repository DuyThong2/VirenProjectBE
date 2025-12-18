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

    [Authorize] // chỉ cần có token
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
    
    
    // ===== Test Admin only =====
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("test-admin")]
    public IResult TestAdmin()
    {
        return TypedResults.Ok("ADMIN_OK");
    }

    // ===== Test User only =====
    [Authorize(Policy = "UserOnly")]
    [HttpGet("test-user")]
    public IResult TestUser()
    {
        return TypedResults.Ok("USER_OK");
    }

    // ===== Test Admin OR User =====
    [Authorize(Policy = "AdminOrUser")]
    [HttpGet("test-admin-user")]
    public IResult TestAdminOrUser()
    {
        return TypedResults.Ok("ADMIN_OR_USER_OK");
    }

    // ===== Existing endpoints =====
    [Authorize(Policy = "AdminOrUser")]
    [HttpGet("profile")]
    public async Task<IResult> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var serviceResponse = await _userService.GetUserByIdAsync(cancellationToken);
        return serviceResponse.Succeeded
            ? TypedResults.Ok(serviceResponse)
            : TypedResults.Unauthorized();
    }

    [Authorize(Policy = "AdminOrUser")]
    [HttpPut("profile")]
    public async Task<IResult> UpdateProfileAsync(
        [FromBody] UserRequestDto requestBody,
        CancellationToken cancellationToken = default)
    {
        var serviceResponse = await _userService.UpdateAsync(requestBody, cancellationToken);
        return serviceResponse.Succeeded
            ? TypedResults.Ok(serviceResponse)
            : TypedResults.BadRequest(serviceResponse);
    }
}