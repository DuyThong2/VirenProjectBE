using Microsoft.AspNetCore.Http;
using Viren.Services.ApiResponse;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;

namespace Viren.Services.Interfaces;

public interface IFitRoomService
{
    Task<FitRoomProxyResponse> CheckModelAsync(IFormFile image, CancellationToken cancellationToken = default);
    Task<FitRoomProxyResponse> CheckClothesAsync(IFormFile image, CancellationToken cancellationToken = default);
    Task<FitRoomProxyResponse> CreateTryOnTaskAsync(FitRoomTryOnRequest request, CancellationToken cancellationToken = default);
    Task<FitRoomProxyResponse> GetTryOnTaskAsync(string taskId, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<FitRoomHistoryDto>> GetMyHistoryAsync(GetFitRoomHistoryRequest request, CancellationToken cancellationToken = default);
}
