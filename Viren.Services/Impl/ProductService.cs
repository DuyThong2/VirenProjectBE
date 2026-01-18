using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductService> _logger;
        private readonly IS3Storage _storage;

        public ProductService(IUnitOfWork unitOfWork, ILogger<ProductService> logger, IS3Storage storage)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _storage = storage;
        }

        // Create new product
        public async Task<ResponseData<Guid>> CreateProductAsync(ProductRequestDto request, CancellationToken cancellationToken = default)
        {
            var productRepository = _unitOfWork.GetRepository<Product, Guid>();

            var existed = await productRepository.Query()
                .AnyAsync(x => x.Name == request.Name, cancellationToken);

            if (existed)
            {
                return new ResponseData<Guid>
                {
                    Succeeded = false,
                    Message = "Sản phẩm đã tồn tại!"
                };
            }

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Detail = request.Detail?.Trim(),
                Care = request.Care?.Trim(),
                Commitment = request.Commitment?.Trim(),
                Thumbnail = request.Thumbnail?.Trim(),
                Price = request.Price,
                Status = request.Status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CategoryId = request.CategoryId
            };

            await productRepository.AddAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ResponseData<Guid>
            {
                Succeeded = true,
                Message = "Tạo sản phẩm thành công!",
                Data = product.Id
            };
        }

        public async Task<ServiceResponse> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var productRepository = _unitOfWork.GetRepository<Product, Guid>();

            var product = await productRepository
                .Query()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (product == null)
            {
                return new ServiceResponse
                {
                    Succeeded = false,
                    Message = "Sản phẩm không tồn tại!"
                };
            }

            product.Status = CommonStatus.Deleted;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResponse
            {
                Message = "Xóa sản phẩm thành công!",
                Succeeded = true
            };

        }

        // Get product and details by id
        public async Task<ServiceResponse> GetProductDetailAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var productRepository = _unitOfWork.GetRepository<Product, Guid>();

            IQueryable<Product> query = productRepository.Query().AsNoTracking();

            var product = await query
                .Where(p => p.Id == productId)
                .Select(p => new ProductWithDetailResponse
                {
                    Id = p.Id,
                    CategoryId = p.CategoryId,
                    Name = p.Name,
                    Description = p.Description,
                    Detail = p.Detail,
                    Care = p.Care,
                    Commitment = p.Commitment,
                    Price = p.Price,
                    Status = p.Status.ToString(),
                    Thumbnail = p.Thumbnail,
                    ProductDetails = p.ProductDetails.Select(pd => new ProductDetailResponseDto
                    {
                        Id = pd.Id,
                        Size = pd.Size,
                        Color = pd.Color,
                        Stock = pd.Stock,
                        Images = pd.Images,
                        CreatedAt = pd.CreatedAt,
                        UpdatedAt = pd.UpdatedAt,
                    }).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found", productId);
                return new ServiceResponse
                {
                    Succeeded = false,
                    Message = "Sản phẩm không tồn tại!"
                };
            }
            _logger.LogInformation("Retrieved details for product ID {ProductId}", productId);
            return new ResponseData<ProductWithDetailResponse>
            {
                Succeeded = true,
                Message = "Lấy chi tiết sản phẩm thành công!",
                Data = product
            };
        }

        // Get all products with pagination, search, and sorting
        public async Task<PaginatedResponse<ProductResponseDto>> GetProductsAsync(GetProductPaginatedRequest request, CancellationToken cancellationToken = default)
        {
            var productRepository = _unitOfWork.GetRepository<Product, Guid>();

            IQueryable<Product> query = productRepository
                .Query()
                .AsNoTracking();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var keyword = request.Search.Trim();
                query = query.Where(p =>
                    p.Name.Contains(keyword)
                );
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                bool ascending = string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
                query = request.SortBy.ToLower() switch
                {
                    "name" => ascending ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name),
                    "price" => ascending ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price),
                    "createdat" => ascending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
                    "status" => ascending ? query.OrderBy(p => p.Status) : query.OrderByDescending(p => p.Status),
                    _ => query.OrderByDescending(p => p.CreatedAt),
                };
            }


            // Count total items
            var totalCount = await query.CountAsync(cancellationToken);

            // Fetch paginated data
            var products = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Thumbnail = p.Thumbnail,
                    Price = p.Price,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    Status = p.Status.ToString(),
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResponse<ProductResponseDto>
            {
                Succeeded = true,
                Message = "Lấy danh sách sản phẩm thành công!",
                PageNumber = request.Page,
                PageSize = request.PageSize,
                TotalItems = totalCount,
                Data = products
            };
        }

        // Get products by category with pagination, search, and sorting
        public async Task<PaginatedResponse<ProductResponseDto>> GetProductsByCateAsync(GetProductPaginatedRequest request, CancellationToken cancellationToken = default)
        {
            var categoryRepository = _unitOfWork.GetRepository<Category, Guid>();

            IQueryable<Product> query = categoryRepository
                .Query()
                .AsNoTracking()
                .Where(c => request.CategoryId == null || c.Id == request.CategoryId)
                .SelectMany(c => c.Products);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var keyword = request.Search.Trim();
                query = query.Where(p => p.Name.Contains(keyword));
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                bool ascending = string.Equals(
                    request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

                query = request.SortBy.ToLower() switch
                {
                    "name" => ascending
                        ? query.OrderBy(p => p.Name)
                        : query.OrderByDescending(p => p.Name),

                    "price" => ascending
                        ? query.OrderBy(p => p.Price)
                        : query.OrderByDescending(p => p.Price),

                    "createdat" => ascending
                        ? query.OrderBy(p => p.CreatedAt)
                        : query.OrderByDescending(p => p.CreatedAt),

                    _ => query.OrderByDescending(p => p.CreatedAt)
                };
            }
            else
            {
                // default sort
                query = query.OrderByDescending(p => p.CreatedAt);
            }

            // Count total items
            var totalCount = await query.CountAsync(cancellationToken);

            // Pagination + projection
            var products = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Thumbnail = p.Thumbnail,
                    Price = p.Price,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    Status = p.Status.ToString(),
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResponse<ProductResponseDto>
            {
                Succeeded = true,
                Message = "Lấy danh sách sản phẩm thành công!",
                PageNumber = request.Page,
                PageSize = request.PageSize,
                TotalItems = totalCount,
                Data = products
            };
        }

        
        public async Task<ReconcileResponseDto> ReconcileProductThumbnailAsync(Guid productId, string? keepJson, List<IFormFile>? files, string? meta, CancellationToken ct)
        {
            var productRepository = _unitOfWork.GetRepository<Product, Guid>();

            var product = await productRepository
                .Query()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == productId);

            if (product == null)
            {
                throw new KeyNotFoundException("Product not found");
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
                product.Thumbnail = null;
            }
            else
            {
                desired = desired
                    .Where(d =>
                        !string.IsNullOrWhiteSpace(d.Key) ||
                        (!string.IsNullOrWhiteSpace(d.Url) && d.Url != "[]"))
                    .ToList();
                product.Thumbnail = desired.Count == 0 ? null : FileRefUtils.ToJson(desired);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return new ReconcileResponseDto
            {
                ClaimId = productId,
                Desired = desired,
                UploadedFiles = uploaded.ToList(),
                Meta = meta
            };
        }

        public async Task<ServiceResponse> UpdateProductAsync(Guid id, ProductRequestDto request, CancellationToken cancellationToken = default)
        {
            var productRepository = _unitOfWork.GetRepository<Product, Guid>();

            var product = await productRepository
                .Query()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (product == null)
            {
                return new ServiceResponse
                {
                    Succeeded = false,
                    Message = "Sản phẩm không tồn tại!"
                };
            }

            var existed = await productRepository
                .Query()
                .AnyAsync(x => x.Name == request.Name && x.Id != id, cancellationToken);

            if (existed)
            {
                return new ServiceResponse
                {
                    Succeeded = false,
                    Message = "Sản phẩm đã tồn tại!"
                };
            }

            product.Name = request.Name.Trim();
            product.Description = request.Description?.Trim();
            product.Detail = request.Detail?.Trim();
            product.Care = request.Care?.Trim();
            product.Commitment = request.Commitment?.Trim();
            product.Thumbnail = request.Thumbnail?.Trim();
            product.Price = request.Price;
            product.Status = request.Status;
            product.CategoryId = request.CategoryId;
            product.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResponse
            {
                Succeeded = true,
                Message = "Cập nhật sản phẩm thành công!"
            };
        }
    }
}
