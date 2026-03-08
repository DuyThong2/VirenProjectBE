using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Services.ApiResponse;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;

namespace Viren.Services.Interfaces
{
    public interface IMeshyService
    {
        Task<MeshyProxyResponse> CreateImageTo3DTaskAsync(
            MeshyImageTo3DRequest request,
            CancellationToken cancellationToken = default);

        Task<MeshyProxyResponse> GetImageTo3DTaskAsync(
            string meshyTaskId,
            CancellationToken cancellationToken = default);

        Task<PaginatedResponse<MeshyTaskHistoryDto>> GetMyMeshyTasksAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
