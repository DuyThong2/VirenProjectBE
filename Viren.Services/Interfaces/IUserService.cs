using LexiMon.Service.ApiResponse;
using Viren.Services.Dtos.Requests;

namespace Viren.Services.Interfaces;

public interface IUserService
{
    Task<ServiceResponse> LoginAsync(LoginRequestDto requestBody);
    Task<ServiceResponse> RegisterAsync(RegisterRequestDto requestBody, string role);
    Task<ServiceResponse> GetUserByIdAsync(CancellationToken cancellationToken = default);
    Task<ServiceResponse> UpdateAsync(UserRequestDto requestBody, CancellationToken cancellationToken = default);
}