using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Domains;
using Viren.Repositories.Enums;
using Viren.Repositories.Interfaces;
using Viren.Repositories.Storage.Bucket;
using Viren.Repositories.Utils;
using Viren.Services.ApiResponse;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;
using Viren.Services.Interfaces;

namespace Viren.Services.Impl
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IS3Storage _storage;

        public CategoryService(IUnitOfWork unitOfWork, IS3Storage storage)
        {
            _unitOfWork = unitOfWork;
            _storage = storage;
        }

        public async Task<ResponseData<Guid>> CreateCategoryAsync(CategoryRequestDto request, CancellationToken cancellationToken = default)
        {
            var categoryRepo = _unitOfWork.GetRepository<Category, Guid>();

            var existed = await categoryRepo.Query()
                .AnyAsync(x => x.Name.ToLower() == request.Name.Trim().ToLower(), cancellationToken);

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

        // Xóa cứng
        public async Task<ServiceResponse> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var categoryRepo = _unitOfWork.GetRepository<Category, Guid>();

            var category = await categoryRepo
                .Query()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (category == null)
            {
                return new ServiceResponse
                {
                    Succeeded = false,
                    Message = "Danh mục không tồn tại!"
                };
            }

            await categoryRepo.RemoveAsync(category, cancellationToken);
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

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchTerm = request.Search.Trim().ToLower();
                query = query.Where(c => c.Name.Contains(searchTerm));
            }

            var totalItems = await query.CountAsync(cancellationToken);

            var categories = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .OrderByDescending(c => c.Name)
                .Select(c => new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Thumbnail = c.Thumbnail,
                    Header = c.Header,
                    Description = c.Description,
                    Status = c.Status.ToString(),
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
                    Status = x.Status.ToString(),
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

        public async Task<ReconcileResponseDto> ReconcileCategoryThumbnailAsync(
            Guid categoryId, 
            string? keepJson, 
            List<IFormFile>? files, 
            string? meta, 
            CancellationToken ct)
        {
            var categoryRepo = _unitOfWork.GetRepository<Category, Guid>();

            var category = await categoryRepo
                .Query()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == categoryId, ct);

            if (category == null) 
              throw new Exception("Category not found");
            
            var keep = FileRefUtils.ParseAny(keepJson);

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
                category.Thumbnail = null;
            }
            else
            {
                desired = desired
                    .Where(d =>
                        !string.IsNullOrWhiteSpace(d.Key) ||
                        (!string.IsNullOrWhiteSpace(d.Url) && d.Url != "[]"))
                    .ToList();
                category.Thumbnail = desired.Count == 0 ? null : FileRefUtils.ToJson(desired);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return new ReconcileResponseDto
            {
                ClaimId = categoryId,
                Desired = desired,
                UploadedFiles = uploaded.ToList(),
                Meta = meta
            };
        }

        public async Task<ServiceResponse> UpdateCategoryAsync(Guid id, CategoryRequestDto request, CancellationToken cancellationToken = default)
        {
            var categoryRepo = _unitOfWork.GetRepository<Category, Guid>();

            var category = await categoryRepo
                .Query()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (category == null)
            {
                return new ServiceResponse
                {
                    Succeeded = false,
                    Message = "Danh mục không tồn tại!"
                };
            }

            var existed = await categoryRepo.Query()
                .AnyAsync(x => x.Name.ToLower() == request.Name.Trim().ToLower() && x.Id != id, cancellationToken);

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
