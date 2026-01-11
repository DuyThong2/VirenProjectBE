using Microsoft.AspNetCore.Http;
using Viren.Services.ApiResponse;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;

namespace Viren.Services.Interfaces;

public interface IUserService
{
    Task<ServiceResponse> LoginAsync(LoginRequestDto requestBody);
    Task<ServiceResponse> RegisterAsync(RegisterRequestDto requestBody, string role);
    Task<ServiceResponse> GetUserByIdAsync(Guid? userId = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse> UpdateAsync(Guid? userId, UserRequestDto requestBody, CancellationToken cancellationToken = default);

    Task<ServiceResponse> CreateAsync(UserCreateRequestDto requestBody, CancellationToken ct = default);
    Task<PaginatedResponse<UserWithSubscriptionResponseDto>> GetUsersAsync(
        GetUsersPaginatedRequest request,
        CancellationToken cancellationToken = default);
    
    Task<ReconcileResponseDto> ReconcileUserFilesAsync(
        Guid userId,
        string? keepJson,
        List<IFormFile>? files,
        string? meta,
        CancellationToken ct);

    Task<ServiceResponse> UpdateUserStatusAsync(
    Guid userId,
    UpdateUserStatusRequestDto requestBody,
    CancellationToken cancellationToken = default);


    Task<ServiceResponse> GoogleLoginAsync(GoogleLoginRequestDto requestBody);

    Task<ServiceResponse> ForgotPasswordAsync(ForgotPasswordRequestDto requestBody, CancellationToken ct = default);
    Task<ServiceResponse> ResetPasswordAsync(ResetPasswordRequestDto requestBody, CancellationToken ct = default);

}