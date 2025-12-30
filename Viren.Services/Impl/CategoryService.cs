using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Domains;
using Viren.Repositories.Enums;
using Viren.Repositories.Interfaces;
using Viren.Services.ApiResponse;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;
using Viren.Services.Interfaces;

namespace Viren.Services.Impl
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseData<Guid>> CreateCategoryAsync(CategoryRequestDto request, CancellationToken cancellationToken = default)
        {
            var categoryRepo = _unitOfWork.GetRepository<Category, Guid>();

            var existed = await categoryRepo.Query()
                .AnyAsync(x => x.Name == request.Name, cancellationToken);

            if (existed)
            {
                return new ResponseData<Guid>
                {
                    Succeeded = false,
                    Message = "Danh mục đã tồn tại!"
                };
            }

            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Thumbnail = request.Thumbnail?.Trim(),
                Header = request.Header?.Trim(),
                Status = request.Status
            };

            await categoryRepo.AddAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ResponseData<Guid>
            {
                Succeeded = true,
                Message = "Tạo danh mục thành công!",
                Data = category.Id
            };
        }

        // Chưa hoạt động đang xóa mềm
        public async Task<ServiceResponse> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var categoryRepo = _unitOfWork.GetRepository<Category, Guid>();

            var category = await categoryRepo
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (category == null)
            {
                return new ServiceResponse
                {
                    Succeeded = false,
                    Message = "Danh mục không tồn tại!"
                };
            }

            category.Status = CommonStatus.Deleted;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResponse
            {
                Succeeded = true,
                Message = "Xóa danh mục thành công!"
            };
        }
   
        public async Task<PaginatedResponse<CategoryResponseDto>> GetAllCategoryAsync(GetCategoryPaginatedRequest request, CancellationToken cancellationToken = default)
        {
            var categoryRepo = _unitOfWork.GetRepository<Category, Guid>();

            IQueryable<Category> query = categoryRepo.Query().AsNoTracking();

            var totalItems = await query.CountAsync(cancellationToken);

            var categories = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Thumbnail = c.Thumbnail,
                    Header = c.Header,
                    Description = c.Description,
                    Status = (int)c.Status,
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResponse<CategoryResponseDto>
            {
                Succeeded = true,
                Message = "Lấy danh sách danh mục thành công!",
                PageNumber = request.Page,
                PageSize = request.PageSize,
                TotalItems = totalItems,
                Data = categories
            };
        }

        public async Task<ResponseData<CategoryResponseDto>> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var categoryRepo = _unitOfWork.GetRepository<Category, Guid>();

            var category = await categoryRepo.Query()
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new CategoryResponseDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Thumbnail = x.Thumbnail,
                    Header = x.Header,
                    //Status = (int)x.Status,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (category == null)
            {
                return new ResponseData<CategoryResponseDto>
                {
                    Succeeded = false,
                    Message = "Danh mục không tồn tại!"
                };
            }

            return new ResponseData<CategoryResponseDto>
            {
                Succeeded = true,
                Message = "Lấy danh mục thành công!",
                Data = category
            };
        }

        public async Task<ServiceResponse> UpdateCategoryAsync(CategoryRequestDto request, CancellationToken cancellationToken = default)
        {
            var categoryRepo = _unitOfWork.GetRepository<Category, Guid>();

            var category = await categoryRepo
                .Query()
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (category == null)
            {
                return new ServiceResponse
                {
                    Succeeded = false,
                    Message = "Danh mục không tồn tại!"
                };
            }

            var existed = await categoryRepo.Query()
                .AnyAsync(x => x.Name == request.Name && x.Id != request.Id, cancellationToken);

            if (existed)
            {
                return new ServiceResponse
                {
                    Succeeded = false,
                    Message = "Tên danh mục đã tồn tại!"
                };
            }

            category.Name = request.Name.Trim();
            category.Description = request.Description?.Trim();
            category.Thumbnail = request.Thumbnail?.Trim();
            category.Header = request.Header?.Trim();
            category.Status = request.Status;

            //categoryRepo.Update(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResponse
            {
                Succeeded = true,
                Message = "Cập nhật danh mục thành công!"
            };
        }

        
    }
}
