using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viren.Repositories.Domains;
using Viren.Repositories.Enums;
using Viren.Repositories.Interfaces;
using Viren.Repositories.Storage.Bucket;
using Viren.Services.ApiResponse;
using Viren.Services.Dtos.Requests;
using Viren.Services.IntegrationEvents;
using Viren.Services.Interfaces;
using Viren.Services.Outbox;

namespace Viren.Services.Impl
{
    public class ProductServiceWithVectorOutbox : ProductService, IProductService
    {
        private readonly IUnitOfWork _uow;

        public ProductServiceWithVectorOutbox(
            IUnitOfWork unitOfWork,
            ILogger<ProductService> logger,
            IS3Storage storage)
            : base(unitOfWork, logger, storage)
        {
            _uow = unitOfWork;
        }

        // ---------------------------
        // Create -> call base, then emit reindex events (optional)
        // ---------------------------
        public override async Task<ResponseData<Guid>> CreateProductAsync(
            ProductRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var result = await base.CreateProductAsync(request, cancellationToken);

            // product mới tạo thường chưa có ProductDetail => không cần emit
            // Nhưng nếu bạn muốn emit ngay (trường hợp bạn tạo detail sau), bỏ qua.
            return result;
        }

        // ---------------------------
        // Update -> call base update, then emit ProductDetail.Upserted for all variants
        // ---------------------------
        public override async Task<ServiceResponse> UpdateProductAsync(
            Guid id,
            ProductRequestDto request,
            CancellationToken cancellationToken = default)
        {
            // 1) Update product (base)
            var result = await base.UpdateProductAsync(id, request, cancellationToken);
            if (!result.Succeeded) return result;

            // 2) Load product + category + sales + details để build events
            var productRepo = _uow.GetRepository<Product, Guid>();
            var outboxRepo = _uow.GetRepository<OutboxEvent, Guid>();

            var nowUtc = DateTime.UtcNow;

            var product = await productRepo.Query()
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductSales)
                    .ThenInclude(ps => ps.Sale)
                .Include(p => p.ProductDetails)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (product == null) return result; // vừa update xong mà null thì thôi

            // 3) Emit one ProductDetail.Upserted event per ProductDetail (re-index)
            foreach (var detail in product.ProductDetails)
            {
                var evt = BuildProductDetailUpsertEvent(product, detail, nowUtc);

                await outboxRepo.AddAsync(
                    OutboxFactory.Create(
                        aggregateType: "ProductDetail",
                        aggregateId: detail.Id,
                        eventType: IntegrationEventTypes.ProductDetailUpserted,
                        payload: evt,
                        correlationId: null,
                        partitionKey: product.Id.ToString(),
                        schemaVersion: evt.SchemaVersion),
                    cancellationToken);
            }

            // base.UpdateProductAsync đã SaveChanges rồi.
            // Nhưng outbox events mới add => cần SaveChanges lần nữa.
            await _uow.SaveChangesAsync(cancellationToken);

            return result;
        }

        // ---------------------------
        // Delete (soft delete) -> reindex or delete details in vector db
        // ---------------------------
        public override async Task<ServiceResponse> DeleteProductAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            // 1) Soft delete product (base)
            var result = await base.DeleteProductAsync(id, cancellationToken);
            if (!result.Succeeded) return result;

            // 2) Nếu muốn vector store remove hết variants:
            // Emit ProductDetail.Deleted for each ProductDetail
            var productRepo = _uow.GetRepository<Product, Guid>();
            var outboxRepo = _uow.GetRepository<OutboxEvent, Guid>();

            var deletedAtUtc = DateTime.UtcNow;

            var details = await productRepo.Query()
                .AsNoTracking()
                .Where(p => p.Id == id)
                .SelectMany(p => p.ProductDetails.Select(d => new { p.Id, DetailId = d.Id }))
                .ToListAsync(cancellationToken);

            foreach (var x in details)
            {
                var evt = new ProductDetailDeletedEventV1
                {
                    ProductId = x.Id,
                    ProductDetailId = x.DetailId,
                    DeletedAtUtc = deletedAtUtc,
                    SchemaVersion = 1
                };

                await outboxRepo.AddAsync(
                    OutboxFactory.Create(
                        aggregateType: "ProductDetail",
                        aggregateId: x.DetailId,
                        eventType: IntegrationEventTypes.ProductDetailDeleted,
                        payload: evt,
                        correlationId: null,
                        partitionKey: x.Id.ToString(),
                        schemaVersion: evt.SchemaVersion),
                    cancellationToken);
            }

            await _uow.SaveChangesAsync(cancellationToken);

            return result;
        }

        // ---------------------------
        // Event builder: same shape as your ProductDetailServiceWithVectorOutbox
        // ---------------------------
        private static ProductDetailUpsertedEventV1 BuildProductDetailUpsertEvent(
            Product product,
            ProductDetail detail,
            DateTime nowUtc)
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

                ProductDetailImage = detail.Images,

                Price = product.Price,
                Stock = detail.Stock,

                Status = StatusToText(detail.Status),
                IsSale = isSale,

                Text = BuildVectorText(product, detail),

                UpdatedAtUtc = detail.UpdatedAt,
                SchemaVersion = 1
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
