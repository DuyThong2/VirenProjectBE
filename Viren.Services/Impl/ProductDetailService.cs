using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Domains;
using Viren.Repositories.Interfaces;
using Viren.Repositories.Storage.Bucket;
using Viren.Repositories.Utils;
using Viren.Services.ApiResponse;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;
using Viren.Services.Interfaces;


namespace Viren.Services.Impl
{
    public class ProductDetailService : IProductDetailService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IS3Storage _storage;

        public ProductDetailService(IUnitOfWork unitOfWork, IS3Storage storage)
        {
            _unitOfWork = unitOfWork;
            _storage = storage;
        }

        //Create product detail
        public async Task<ResponseData<Guid>> CreateProductDetailAsync(ProductDetailRequestDto request, CancellationToken cancellationToken = default)
        {
            var productDetailRepo = _unitOfWork.GetRepository<ProductDetail, Guid>();

            //Kiểm tra chi tiết sản phẩm đã tồn tại chưa
            var existed = await productDetailRepo.Query()
                .AnyAsync(x => x.ProductId == request.ProductId && x.Size == request.Size && x.Color == request.Color, cancellationToken);
            if (existed)
            {
                return new ResponseData<Guid>
                {
                    Succeeded = false,
                    Message = "Chi tiết sản phẩm đã tồn tại!"
                };
            }

            //Kiểm tra productId có tồn tại không
            var productExits = await _unitOfWork.GetRepository<Product, Guid>()
                .Query()
                .AnyAsync(p => p.Id == request.ProductId, cancellationToken);
            if (!productExits)
            {
                return new ResponseData<Guid>
                {
                    Succeeded = false,
                    Message = "Sản phẩm không tồn tại!"
                };
            }

            var productDetail = new ProductDetail
            {
                Id = Guid.NewGuid(),
                Size = request.Size.Trim().ToUpper(),
                Color = request.Color.Trim(),
                Stock = request.Stock,
                Images = request.Images,
                Status = request.Status,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                ProductId = request.ProductId
            };

            await productDetailRepo.AddAsync(productDetail, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ResponseData<Guid>
            {
                Succeeded = true,
                Message = "Tạo chi tiết sản phẩm thành công!",
                Data = productDetail.Id
            };
        }

        //Delete product detail
        public async Task<ServiceResponse> DeleteProductDetailAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var productDetailRepo = _unitOfWork.GetRepository<ProductDetail, Guid>();

            var productDetail = await productDetailRepo
                .Query()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (productDetail == null)
            {
                return new ServiceResponse
                {
                    Succeeded = false,
                    Message = "Chi tiết sản phẩm không tồn tại!"
                };
            }

            await productDetailRepo.RemoveAsync(productDetail, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResponse
            {
                Succeeded = true,
                Message = "Xóa chi tiết sản phẩm thành công!"
            };
        }

        //Get all product details with pagination, filtering, and sorting
        public async Task<PaginatedResponse<ProductDetailResponseDto>> GetAllProductDetailAsync(GetProductDetailPaginatedRequest request, CancellationToken cancellationToken = default)
        {
            var productDetailRepo = _unitOfWork.GetRepository<ProductDetail, Guid>();

            var query = productDetailRepo
                .Query()
                .AsNoTracking();

            // Filter by ProductId
            if (request.ProductId.HasValue)
            {
                query = query.Where(p => p.ProductId == request.ProductId.Value);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var keyword = request.Search.Trim();
                query = query.Where(p =>
                    p.Color.Contains(keyword) ||
                    p.Size.Contains(keyword)
                );
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                bool ascending = string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
                query = request.SortBy.ToLower() switch
                {
                    "createdAt" => ascending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
                    "updatedAt" => ascending ? query.OrderBy(p => p.UpdatedAt) : query.OrderByDescending(p => p.UpdatedAt),
                    "size" => ascending ? query.OrderBy(p => p.Size) : query.OrderByDescending(p => p.Size),
                    "color" => ascending ? query.OrderBy(p => p.Color) : query.OrderByDescending(p => p.Color),
                    "stock" => ascending ? query.OrderBy(p => p.Stock) : query.OrderByDescending(p => p.Stock),
                    _ => query.OrderByDescending(p => p.CreatedAt),
                };
            }

            // Get total count before pagination
            var totalItems = await query.CountAsync(cancellationToken);
            var productDetails = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new ProductDetailResponseDto
                {
                    Id = p.Id,
                    Size = p.Size,
                    Color = p.Color,
                    Stock = p.Stock,
                    Images = p.Images,
                    Status = p.Status.ToString(),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResponse<ProductDetailResponseDto>
            {
                Data = productDetails,
                Succeeded = true,
                Message = "Lấy danh sách chi tiết sản phẩm thành công!",
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalItems = totalItems
            };
        }

        //Get product detail by id
        public async Task<ResponseData<ProductDetailResponseDto>> GetProductDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var productDetailRepo = _unitOfWork.GetRepository<ProductDetail, Guid>();

            var productDetail = await productDetailRepo.Query()
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new ProductDetailResponseDto
                {
                    Id = p.Id,
                    Size = p.Size,
                    Color = p.Color,
                    Stock = p.Stock,
                    Images = p.Images,
                    Status = p.Status.ToString(),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (productDetail == null)
            {
                return new ResponseData<ProductDetailResponseDto>
                {
                    Succeeded = false,
                    Message = "Chi tiết sản phẩm không tồn tại!"
                };
            }

            return new ResponseData<ProductDetailResponseDto>
            {
                Succeeded = true,
                Message = "Lấy chi tiết sản phẩm thành công!",
                Data = productDetail
            };
        }

        public async Task<ReconcileResponseDto> ReconcileProductDetailThumbnailAsync(Guid productDetailId, string? keepJson, List<IFormFile>? files, string? meta, CancellationToken ct)
        {
            var productDetailRepo = _unitOfWork.GetRepository<ProductDetail, Guid>();

            var productDetail = await productDetailRepo
                .Query()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == productDetailId);

            if (productDetail == null)
            {
                throw new Exception("Product detail not found");
            }

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
                productDetail.Images = null;
            }
            else
            {
                desired = desired
                    .Where(d =>
                        !string.IsNullOrWhiteSpace(d.Key) ||
                        (!string.IsNullOrWhiteSpace(d.Url) && d.Url != "[]"))
                    .ToList();
                productDetail.Images = desired.Count == 0 ? null : FileRefUtils.ToJson(desired);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return new ReconcileResponseDto
            {
                ClaimId = productDetailId,
                Desired = desired,
                UploadedFiles = uploaded.ToList(),
                Meta = meta
            };
        }

        //Update product detail
        public async Task<ServiceResponse> UpdateProductDetailAsync(Guid id, ProductDetailRequestDto request, CancellationToken cancellationToken = default)
        {
            var productDetailRepo = _unitOfWork.GetRepository<ProductDetail, Guid>();

            var productDetail = await productDetailRepo
                .Query()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (productDetail == null)
            {
                return new ServiceResponse
                {
                    Succeeded = false,
                    Message = "Chi tiết sản phẩm không tồn tại!"
                };
            }

            var existed = await productDetailRepo.Query()
                .AnyAsync(x => x.Id != id && x.ProductId == request.ProductId && x.Size == request.Size && x.Color == request.Color, cancellationToken);

            if (existed)
            {
                return new ServiceResponse
                {
                    Succeeded = false,
                    Message = "Chi tiết sản phẩm đã tồn tại!"
                };
            }

            productDetail.Size = request.Size.Trim().ToUpper();
            productDetail.Color = request.Color.Trim();
            productDetail.Stock = request.Stock;
            productDetail.Images = request.Images;
            productDetail.Status = request.Status;
            productDetail.UpdatedAt = DateTime.Now;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new ServiceResponse
            {
                Succeeded = true,
                Message = "Cập nhật chi tiết sản phẩm thành công!"
            };
        }
    }
}
