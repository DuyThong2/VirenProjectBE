using Azure.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Domains;
using Viren.Repositories.Interfaces;
using Viren.Repositories.Utils;
using Viren.Services.ApiResponse;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;
using Viren.Services.Interfaces;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Viren.Services.Impl
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IUnitOfWork unitOfWork, ILogger<ProductService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ServiceResponse> GetProductDetailAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var productRepository = _unitOfWork.GetRepository<Product, Guid>();

            IQueryable<Product> query = productRepository.Query().AsNoTracking();

            var product = await query
                .Where(p => p.Id == productId)
                .Select(p => new ProductWithDetailResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Detail = p.Detail,
                    Care = p.Care,
                    Commitment = p.Commitment,
                    Price = p.Price,
                    ProductDetails = p.ProductDetails.Select(pd => new ProductDetailResponse
                    {
                        Id = pd.Id,
                        Size = pd.Size,
                        Color = pd.Color,
                        Stock = pd.Stock,
                        Images = pd.Images
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

        public async Task<PaginatedResponse<ProductResponseDto>> GetProductsAsync(ProductRequestDto request, CancellationToken cancellationToken = default)
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
                    UpdatedAt = p.UpdatedAt
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

        public async Task<PaginatedResponse<ProductResponseDto>> GetProductsByCateAsync(ProductRequestDto request, CancellationToken cancellationToken = default)
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
                    UpdatedAt = p.UpdatedAt
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
    }
}
