using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Viren.Repositories.Domains;
using Viren.Repositories.Enums;
using Viren.Repositories.Interfaces;
using Viren.Repositories.Storage.Bucket;
using Viren.Repositories.Utils;
using Viren.Services.ApiResponse;
using Viren.Services.Dtos.Requests;
using Viren.Services.Dtos.Response;
using Viren.Services.IntegrationEvents;
using Viren.Services.Interfaces;
using Viren.Services.Outbox;

namespace Viren.Services.Impl
{
    public class ProductDetailServiceWithVectorOutbox : ProductDetailService, IProductDetailService
    {
        public ProductDetailServiceWithVectorOutbox(IUnitOfWork unitOfWork, IS3Storage storage)
            : base(unitOfWork, storage)
        {
        }

        public override async Task<ResponseData<Guid>> CreateProductDetailAsync(
            ProductDetailRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var productDetailRepo = _unitOfWork.GetRepository<ProductDetail, Guid>();
            var outboxRepo = _unitOfWork.GetRepository<OutboxEvent, Guid>();
            var productRepo = _unitOfWork.GetRepository<Product, Guid>();

            var existed = await productDetailRepo.Query()
                .AnyAsync(x => x.ProductId == request.ProductId
                            && x.Size == request.Size
                            && x.Color == request.Color, cancellationToken);

            if (existed)
                return new ResponseData<Guid> { Succeeded = false, Message = "Chi tiết sản phẩm đã tồn tại!" };

            // Load Product + Category + Sales
            var product = await productRepo.Query()
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductSales)
                    .ThenInclude(ps => ps.Sale)
                .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

            if (product == null)
                return new ResponseData<Guid> { Succeeded = false, Message = "Sản phẩm không tồn tại!" };

            var nowUtc = DateTime.UtcNow;

            var productDetail = new ProductDetail
            {
                Id = Guid.NewGuid(),
                Size = request.Size.Trim().ToUpperInvariant(),
                Color = request.Color.Trim(),
                Stock = request.Stock,
                Images = request.Images,
                Status = request.Status,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc,
                ProductId = request.ProductId
            };

            await productDetailRepo.AddAsync(productDetail, cancellationToken);

            var evt = BuildUpsertEvent(product, productDetail, nowUtc);

            await outboxRepo.AddAsync(
                OutboxFactory.Create(
                    aggregateType: "ProductDetail",
                    aggregateId: productDetail.Id,
                    eventType: IntegrationEventTypes.ProductDetailUpserted,
                    payload: evt,
                    correlationId: null,
                    partitionKey: productDetail.ProductId.ToString(),
                    schemaVersion: evt.SchemaVersion),
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ResponseData<Guid>
            {
                Succeeded = true,
                Message = "Tạo chi tiết sản phẩm thành công!",
                Data = productDetail.Id
            };
        }

        public override async Task<ServiceResponse> UpdateProductDetailAsync(
            Guid id,
            ProductDetailRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var productDetailRepo = _unitOfWork.GetRepository<ProductDetail, Guid>();
            var outboxRepo = _unitOfWork.GetRepository<OutboxEvent, Guid>();
            var productRepo = _unitOfWork.GetRepository<Product, Guid>();

            var productDetail = await productDetailRepo.Query()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (productDetail == null)
                return new ServiceResponse { Succeeded = false, Message = "Chi tiết sản phẩm không tồn tại!" };

            var existed = await productDetailRepo.Query()
                .AnyAsync(x => x.Id != id
                            && x.ProductId == request.ProductId
                            && x.Size == request.Size
                            && x.Color == request.Color, cancellationToken);

            if (existed)
                return new ServiceResponse { Succeeded = false, Message = "Chi tiết sản phẩm đã tồn tại!" };

            var nowUtc = DateTime.UtcNow;

            productDetail.Size = request.Size.Trim().ToUpperInvariant();
            productDetail.Color = request.Color.Trim();
            productDetail.Stock = request.Stock;
            productDetail.Images = request.Images;
            productDetail.Status = request.Status;
            productDetail.UpdatedAt = nowUtc;

            // Load Product + Category + Sales
            var product = await productRepo.Query()
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductSales)
                    .ThenInclude(ps => ps.Sale)
                .FirstOrDefaultAsync(p => p.Id == productDetail.ProductId, cancellationToken);

            if (product == null)
                return new ServiceResponse { Succeeded = false, Message = "Sản phẩm không tồn tại!" };

            var evt = BuildUpsertEvent(product, productDetail, nowUtc);

            await outboxRepo.AddAsync(
                OutboxFactory.Create(
                    aggregateType: "ProductDetail",
                    aggregateId: productDetail.Id,
                    eventType: IntegrationEventTypes.ProductDetailUpserted,
                    payload: evt,
                    correlationId: null,
                    partitionKey: productDetail.ProductId.ToString(),
                    schemaVersion: evt.SchemaVersion),
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResponse { Succeeded = true, Message = "Cập nhật chi tiết sản phẩm thành công!" };
        }

        public override async Task<ServiceResponse> DeleteProductDetailAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var productDetailRepo = _unitOfWork.GetRepository<ProductDetail, Guid>();
            var outboxRepo = _unitOfWork.GetRepository<OutboxEvent, Guid>();

            var productDetail = await productDetailRepo.Query()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (productDetail == null)
                return new ServiceResponse { Succeeded = false, Message = "Chi tiết sản phẩm không tồn tại!" };

            var deletedAtUtc = DateTime.UtcNow;

            await productDetailRepo.RemoveAsync(productDetail, cancellationToken);

            var evt = new ProductDetailDeletedEventV1
            {
                ProductId = productDetail.ProductId,
                ProductDetailId = productDetail.Id,
                DeletedAtUtc = deletedAtUtc,
                SchemaVersion = 1
            };

            await outboxRepo.AddAsync(
                OutboxFactory.Create(
                    aggregateType: "ProductDetail",
                    aggregateId: productDetail.Id,
                    eventType: IntegrationEventTypes.ProductDetailDeleted,
                    payload: evt,
                    correlationId: null,
                    partitionKey: productDetail.ProductId.ToString(),
                    schemaVersion: evt.SchemaVersion),
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResponse { Succeeded = true, Message = "Xóa chi tiết sản phẩm thành công!" };
        }

        // ------------------------------
        // Build payload for vector
        // ------------------------------

        private static ProductDetailUpsertedEventV1 BuildUpsertEvent(Product product, ProductDetail detail, DateTime nowUtc)
        {
            var isSale = IsProductOnSaleNow(product, nowUtc);

            return new ProductDetailUpsertedEventV1
            {
                ProductId = product.Id,
                ProductDetailId = detail.Id,

                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? "",

                ProductName = product.Name ?? "",
                ProductDescription = product.Description ?? "",

                Size = detail.Size,
                ColorRaw = detail.Color,
                ColorFamily = InferColorFamily(detail.Color),

                Price = product.Price,
                Stock = detail.Stock,
                ProductDetailImage = detail.Images,


                // FIX: luôn ra text enum
                Status = StatusToText(detail.Status),

                IsSale = isSale,

                Text = BuildVectorText(product, detail),

                UpdatedAtUtc = detail.UpdatedAt,
                SchemaVersion = 1
            };
        }

        public override async Task<ReconcileResponseDto> ReconcileProductDetailThumbnailAsync(
    Guid productDetailId,
    string? keepJson,
    List<IFormFile>? files,
    string? meta,
    CancellationToken ct)
        {
            var productDetailRepo = _unitOfWork.GetRepository<ProductDetail, Guid>();
            var productRepo = _unitOfWork.GetRepository<Product, Guid>();
            var outboxRepo = _unitOfWork.GetRepository<OutboxEvent, Guid>();

            // 1) Load detail (tracking) để update Images
            var productDetail = await productDetailRepo
                .Query()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == productDetailId, ct);

            if (productDetail == null)
                throw new Exception("Product detail not found");

            // 2) reconcile ảnh như bạn đang làm
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

            // 3) set UpdatedAt để vector biết record mới
            var nowUtc = DateTime.UtcNow;
            productDetail.UpdatedAt = nowUtc;

            // 4) Load product để build payload giống Upsert
            var product = await productRepo.Query()
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductSales)
                    .ThenInclude(ps => ps.Sale)
                .FirstOrDefaultAsync(p => p.Id == productDetail.ProductId, ct);

            if (product == null)
                throw new Exception("Product not found");

            // 5) Add outbox Upsert event (chung loại event với Update)
            var evt = BuildUpsertEvent(product, productDetail, nowUtc);

            await outboxRepo.AddAsync(
                OutboxFactory.Create(
                    aggregateType: "ProductDetail",
                    aggregateId: productDetail.Id,
                    eventType: IntegrationEventTypes.ProductDetailUpserted,
                    payload: evt,
                    correlationId: null,
                    partitionKey: productDetail.ProductId.ToString(),
                    schemaVersion: evt.SchemaVersion),
                ct);

            // 6) Commit 1 lần: update Images + insert OutboxEvent
            await _unitOfWork.SaveChangesAsync(ct);

            return new ReconcileResponseDto
            {
                ClaimId = productDetailId,
                Desired = desired,
                UploadedFiles = uploaded.ToList(),
                Meta = meta
            };
        }

        private static string BuildVectorText(Product product, ProductDetail detail)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(product.Description))
                parts.Add(product.Description.Trim());

            if (!string.IsNullOrWhiteSpace(product.Name))
                parts.Add(product.Name.Trim());

            if (!string.IsNullOrWhiteSpace(product.Detail))
                parts.Add(product.Detail.Trim());

            parts.Add($"Size: {detail.Size}");
            parts.Add($"Color: {detail.Color}");

            return string.Join(" \n ", parts);
        }

        private static string StatusToText(CommonStatus status)
        {
            return status switch
            {
                CommonStatus.Active => "Active",
                CommonStatus.Inactive => "Inactive",
                CommonStatus.Deleted => "Deleted",
                _ => "Unknown"
            };
        }

        private static string InferColorFamily(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "unknown";
            var s = raw.Trim().ToLowerInvariant();

            if (s.Contains("đỏ") || s.Contains("red")) return "red";
            if (s.Contains("xanh") || s.Contains("blue")) return "blue";
            if (s.Contains("đen") || s.Contains("black")) return "black";
            if (s.Contains("trắng") || s.Contains("white")) return "white";
            if (s.Contains("vàng") || s.Contains("yellow")) return "yellow";
            if (s.Contains("hồng") || s.Contains("pink")) return "pink";
            if (s.Contains("nâu") || s.Contains("brown")) return "brown";
            if (s.Contains("xám") || s.Contains("gray") || s.Contains("grey")) return "gray";

            return "other";
        }

        private static bool IsProductOnSaleNow(Product product, DateTime nowUtc)
        {
            return product.ProductSales?.Any(ps =>
                ps.Sale != null
                && ps.Sale.Status == CommonStatus.Active
                && ps.Sale.StartDate <= nowUtc
                && ps.Sale.EndDate >= nowUtc
            ) == true;
        }
    }
}
