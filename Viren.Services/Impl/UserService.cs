using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viren.Repositories.Domains;
using Viren.Repositories.Enums;
using Viren.Repositories.Interfaces;
using Viren.Repositories.Storage.Bucket;
using Viren.Repositories.Utils;
using Viren.Services.ApiResponse;
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
    private readonly IS3Storage _storage;


    public UserService(UserManager<User> userManager, ITokenRepository tokenRepository, IUnitOfWork unitOfWork, ILogger<UserService> logger, IUser user, IS3Storage storage)
    {
        _userManager = userManager;
        _tokenRepository = tokenRepository;
        _logger = logger;
        _user = user;
        _unitOfWork = unitOfWork;
        _storage = storage;
    }

    public async Task<ServiceResponse> LoginAsync(LoginRequestDto requestBody)
    {
        User? user;

        // Detect email vs username
        if (requestBody.EmailOrUsername.Contains("@"))
        {
            user = await _userManager.FindByEmailAsync(requestBody.EmailOrUsername);
        }
        else
        {
            user = await _userManager.FindByNameAsync(requestBody.EmailOrUsername);
        }
        
        if (user == null || !await _userManager.CheckPasswordAsync(user, requestBody.Password))
        {
            _logger.LogWarning("Failed login attempt for email: {Email}", requestBody.EmailOrUsername);
            return new ServiceResponse()
            {
                Succeeded = false,
                Message = "Sai email hoặc mật khẩu!",
            };
        }

        if (!(user.Status == CommonStatus.Active))
        {
            _logger.LogWarning("Disabled account login attempt for email: {Email}", requestBody.EmailOrUsername);
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

        _logger.LogInformation("User {Email} logged in successfully with role {Role}", requestBody.EmailOrUsername, role);
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
            Name = requestBody.Email
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

        var addToRoleResult = await _userManager.AddToRoleAsync(user, "User");
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

    public async Task<ServiceResponse> GetUserByIdAsync(
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedUserId = ResolveUserId(userId);

        var user = await _userManager.FindByIdAsync(resolvedUserId.ToString());
        if (user == null)
        {
            _logger.LogError("User not found with ID: {UserId}", resolvedUserId);
            return new ServiceResponse
            {
                Succeeded = false,
                Message = "Người dùng không tồn tại!"
            };
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "User";

        var response = new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email!,
            UserName = user.UserName,          // ✅
            Role = role,

            Name = user.Name,                  // ✅
            PhoneNumber = user.PhoneNumber,    // ✅

            FirstName = user.FirstName,
            LastName = user.LastName,
            Address = user.Address,

            Gender = user.Gender,
            Height = user.Height,
            Weight = user.Weight,

            Status = (int) user.Status, // nếu request.Status là int
            AvatarImg = user.AvatarImg,
            BirthDate = user.Birthdate,
            CreatedAt = user.CreatedAt
        };

        _logger.LogInformation("User information retrieved successfully for ID: {UserId}", resolvedUserId);

        return new ResponseData<UserResponseDto>
        {
            Succeeded = true,
            Message = "Lấy thông tin người dùng thành công!",
            Data = response
        };
    }



    public async Task<ServiceResponse> UpdateAsync(
    Guid? userId,
    UserRequestDto requestBody,
    CancellationToken cancellationToken = default)
{
    var resolvedUserId = ResolveUserId(userId);

    var user = await _userManager.FindByIdAsync(resolvedUserId.ToString());
    if (user == null)
    {
        return new ServiceResponse
        {
            Succeeded = false,
            Message = "Người dùng không tồn tại!"
        };
    }

    user.Name = requestBody.Name?.Trim() ?? user.Name;
    user.PhoneNumber = requestBody.PhoneNumber?.Trim();
    user.Birthdate = requestBody.BirthDate;
    user.Height = requestBody.Height;
    user.Weight = requestBody.Weight;
    user.Gender = requestBody.Gender.HasValue ? requestBody.Gender.Value : user.Gender;

    user.FirstName = requestBody.FirstName?.Trim();
    user.LastName = requestBody.LastName?.Trim();
    user.Address = requestBody.Address?.Trim();
    Console.WriteLine("beyound value");
    if (requestBody.Status.HasValue)
        user.Status = requestBody.Status.Value;
    // ✅ IMPORTANT: đồng bộ Name -> UserName để login
    if (!string.IsNullOrWhiteSpace(requestBody.Name))
    {
        var newUserName = requestBody.Name.Trim();

        // nếu muốn "slugify" cho username (không dấu, không space), bạn có thể thay ở đây
        // newUserName = Slugify(newUserName);

        // check trùng username với user khác
        var existed = await _userManager.FindByNameAsync(newUserName);
        if (existed != null && existed.Id != user.Id)
        {
            return new ServiceResponse
            {
                Succeeded = false,
                Message = "Tên đăng nhập (UserName) đã tồn tại. Vui lòng chọn tên khác."
            };
        }

        user.UserName = newUserName;
        user.NormalizedUserName = _userManager.NormalizeName(newUserName);
    }

    // update qua UserManager để Identity set stamp đúng
    var result = await _userManager.UpdateAsync(user);
    if (!result.Succeeded)
    {
        var errors = string.Join("; ", result.Errors.Select(e => e.Description));
        return new ServiceResponse
        {
            Succeeded = false,
            Message = $"Cập nhật thất bại: {errors}"
        };
    }

    return new ServiceResponse
    {
        Succeeded = true,
        Message = "Cập nhật người dùng thành công!"
    };
}



    public async Task<PaginatedResponse<UserWithSubscriptionResponseDto>> GetUsersAsync(
        GetUsersPaginatedRequest request,
        CancellationToken cancellationToken = default)
    {
        var userRepo = _unitOfWork.GetRepository<User, Guid>();

        IQueryable<User> query = userRepo.Query().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim();
            query = query.Where(u =>
                u.Name.Contains(keyword) ||
                u.Email!.Contains(keyword) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(keyword))
            );
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserWithSubscriptionResponseDto
            {
                LastName =  u.LastName,
                FirstName = u.FirstName,
                ImgUrl = u.AvatarImg,
                Id = u.Id,
                Email = u.Email!,
                Name = u.Name,
                PhoneNumber = u.PhoneNumber,
                CreatedAt = u.CreatedAt,
                status = u.Status.ToString(),
                SubscriptionName = u.UserSubscriptions
                    .Where(us => us.Status == CommonStatus.Active)
                    .OrderByDescending(us => us.EndDate)
                    .Select(us => us.SubscriptionPlan.Name)
                    .ToList()
            })
            .ToListAsync(cancellationToken);


        return new PaginatedResponse<UserWithSubscriptionResponseDto>
        {
            Succeeded = true,
            Message = "Lấy danh sách người dùng thành công",
            Data = users,
            PageSize = request.PageSize,
            TotalItems = totalCount
        };
    }


    private string ResolveUserId(Guid? userId)
    {
        if (userId.HasValue)
            return userId.Value.ToString();

        if (!string.IsNullOrEmpty(_user.Id))
            return _user.Id;

        throw new UnauthorizedAccessException("User is not authenticated");
    }
    
     public async Task<ReconcileResponseDto> ReconcileUserFilesAsync(
        Guid userId,
        string? keepJson,
        List<IFormFile>? files,
        string? meta,
        CancellationToken ct)
    {
        // 0) load user
        var resolvedUserId = ResolveUserId(userId);

        var user = await _userManager.FindByIdAsync(resolvedUserId);      
        if (user is null)
            throw new KeyNotFoundException($"User {userId} not found.");

        // 1) giữ lại từ KeepJson
        var keep = FileRefUtils.ParseAny(keepJson);

        // 2) upload file mới (nếu có)
        var uploaded = (files is { Count: > 0 })
            ? await _storage.UploadAsync(files!, ct)
            : Array.Empty<UploadedFileDto>();

        List<FileRefDto> desired;
        if (uploaded.Count == 0)
        {
            desired = FileRefUtils.Distinct(keep);
        }
        else
        {
            var newRefs = FileRefUtils.FromUploaded(uploaded);
            desired = FileRefUtils.Distinct(keep.Concat(newRefs));
        }

       
        if ((keep == null || keep.Count == 0) && uploaded.Count == 0)
        {
            user.AvatarImg = null;
        }
        else
        {
            desired = desired
                .Where(d =>
                    !string.IsNullOrWhiteSpace(d.Key) ||
                    (!string.IsNullOrWhiteSpace(d.Url) && d.Url != "[]"))
                .ToList();

            user.AvatarImg = desired.Count == 0 ? null : FileRefUtils.ToJson(desired);
        }

        await _userManager.UpdateAsync(user);

        return new ReconcileResponseDto
        {
            
            Desired = desired,
            UploadedFiles = uploaded.ToList(),
            Meta = meta
        };
    }
    
    
    

}