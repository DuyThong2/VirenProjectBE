using LexiMon.Service.ApiResponse;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Viren.Repositories.Domains;
using Viren.Repositories.Enums;
using Viren.Repositories.Interfaces;
using Viren.Repositories.Utils;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;
using Viren.Services.Interfaces;

namespace Viren.Services.Impl;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenRepository _tokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUser _user;
    private readonly ILogger<UserService> _logger;

    public UserService(UserManager<User> userManager, ITokenRepository tokenRepository, IUnitOfWork unitOfWork, ILogger<UserService> logger, IUser user)
    {
        _userManager = userManager;
        _tokenRepository = tokenRepository;
        _logger = logger;
        _user = user;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResponse> LoginAsync(LoginRequestDto requestBody)
    {
        var user = await _userManager.FindByEmailAsync(requestBody.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, requestBody.Password))
        {
            _logger.LogWarning("Failed login attempt for email: {Email}", requestBody.Email);
            return new ServiceResponse()
            {
                Succeeded = false,
                Message = "Sai email hoặc mật khẩu!",
            };
        }

        if (!(user.Status == CommonStatus.Active))
        {
            _logger.LogWarning("Disabled account login attempt for email: {Email}", requestBody.Email);
            return new ServiceResponse()
            {
                Succeeded = false,
                Message = "Tài khoản đã bị vô hiệu hóa!",
            };
        }

        var role = (await _userManager.GetRolesAsync(user))[0];
        var (token, expire) = _tokenRepository.GenerateJwtToken(user, role);

        var response = new LoginResponseDto()
        {
            Token = token,
            ExpiredIn = expire,
        };

        _logger.LogInformation("User {Email} logged in successfully with role {Role}", requestBody.Email, role);
        return new ResponseData<LoginResponseDto>()
        {
            Succeeded = true,
            Message = "Đăng nhập thành công!",
            Data = response
        };
    }

    public async Task<ServiceResponse> RegisterAsync(RegisterRequestDto requestBody, string role)
    {
        var user = await _userManager.FindByEmailAsync(requestBody.Email);
        if (user != null)
        {
            return new ServiceResponse()
            {
                Succeeded = false,
                Message = "Tài khoản đã tồn tại!",
            };
        }

        user = new User()
        {
            UserName = requestBody.Email,
            Email = requestBody.Email,
            FirstName = requestBody.FirstName,
            LastName = requestBody.LastName,
        };

        var result = await _userManager.CreateAsync(user, requestBody.Password);
        if (!result.Succeeded)
        {
            return new ServiceResponse
            {
                Succeeded = false,
                Message = "Không thể tạo tài khoản. Vui lòng thử lại!",
            };
        }

        var addToRoleResult = await _userManager.AddToRoleAsync(user, role);
        if (!addToRoleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return new ServiceResponse
            {
                Succeeded = false,
                Message = "Không thể tạo tài khoản. Vui lòng thử lại!",
            };
        }

        
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User {Email} registered successfully with role {Role}", requestBody.Email, role);
        return new ResponseData<Guid>()
        {
            Succeeded = true,
            Message = "Tạo tài khoản thành công!",
            Data = user.Id
        };
    }

    public async Task<ServiceResponse> GetUserByIdAsync(CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(_user.Id!);

        if (user == null)
        {
            _logger.LogError("User not found with ID: {UserId}", _user.Id);
            return new ServiceResponse()
            {
                Succeeded = false,
                Message = "Người dùng không tồn tại!",
            };
        }

        var role = (await _userManager.GetRolesAsync(user))[0];

        var response = new UserResponseDto()
        {
            Id = user.Id,
            Email = user.Email!,
            Role = role,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Address = user.Address,
            BirthDate = user.Birthdate,
            
        };

        _logger.LogInformation("User information retrieved successfully for ID: {UserId}", _user.Id);
        return new ResponseData<UserResponseDto>()
        {
            Succeeded = true,
            Message = "Lấy thông tin người dùng thành công!",
            Data = response
        };
    }

    public async Task<ServiceResponse> UpdateAsync(UserRequestDto requestBody,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(_user.Id!);

        if (user == null)
        {
            _logger.LogError("User not found with ID: {UserId}", _user.Id);
            return new ServiceResponse()
            {
                Succeeded = false,
                Message = "Người dùng không tồn tại!",
            };
        }

        user.FirstName = requestBody.FirstName;
        user.LastName = requestBody.LastName;
        user.Address = requestBody.Address;
        user.Birthdate = requestBody.BirthDate;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to update user with ID: {UserId}", _user.Id);
            return new ServiceResponse()
            {
                Succeeded = false,
                Message = "Cập nhật thông tin người dùng thất bại!",
            };
        }

        _logger.LogInformation("User information updated successfully for ID: {UserId}", _user.Id);
        return new ServiceResponse()
        {
            Succeeded = true,
            Message = "Cập nhật thông tin người dùng thành công!",
        };
    }

   
}