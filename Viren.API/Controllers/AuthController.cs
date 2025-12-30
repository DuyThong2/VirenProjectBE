using Microsoft.AspNetCore.Mvc;
using Viren.Services.Dtos.Requests;
using Viren.Services.Interfaces;

namespace Viren.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthsController
{
    private readonly IUserService _userService;

    public AuthsController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("login")]
    public async Task<IResult> LoginAsync([FromBody] LoginRequestDto requestBody)
    {
        var serviceResponse = await _userService.LoginAsync(requestBody);
        if (serviceResponse.Succeeded)
        {
            return TypedResults.Ok(serviceResponse);
        }

        return TypedResults.Unauthorized();
    }

    [HttpPost("{role}/register")]
    public async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequestDto requestBody,
        [FromRoute] string role)
    {
        var serviceResponse = await _userService.RegisterAsync(requestBody, role);
        if (serviceResponse.Succeeded)
        {
            return TypedResults.Ok(serviceResponse);
        }

        return TypedResults.BadRequest(serviceResponse);
    }
    
    
    [HttpPost("google")]
    public async Task<IResult> GoogleLoginAsync([FromBody] GoogleLoginRequestDto requestBody)
    {
        var serviceResponse = await _userService.GoogleLoginAsync(requestBody);

        if (serviceResponse.Succeeded)
            return TypedResults.Ok(serviceResponse);

        return TypedResults.Unauthorized();
    }

}