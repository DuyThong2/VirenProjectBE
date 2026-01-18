using Microsoft.EntityFrameworkCore;
using Viren.Repositories.Domains;
using Viren.Repositories.Impl;
using Viren.Repositories.Interfaces;
using Viren.Repositories.Storage.Bucket;
using Viren.Services.ApiResponse;
using Viren.Services.Dtos.Requests;
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

            var existed = await productDetailRepo.Query()
                .AnyAsync(x => x.ProductId == request.ProductId
                            && x.Size == request.Size
                            && x.Color == request.Color, cancellationToken);

            if (existed)
            {
                return new ResponseData<Guid> { Succeeded = false, Message = "Chi tiết sản phẩm đã tồn tại!" };
            }

            var productExists = await _unitOfWork.GetRepository<Product, Guid>()
                .Query()
                .AnyAsync(p => p.Id == request.ProductId, cancellationToken);

            if (!productExists)
            {
                return new ResponseData<Guid> { Succeeded = false, Message = "Sản phẩm không tồn tại!" };
            }

            var nowUtc = DateTime.UtcNow;

            var productDetail = new ProductDetail
            {
                Id = Guid.NewGuid(),
                Size = request.Size.Trim().ToUpper(),
                Color = request.Color.Trim(),
                Stock = request.Stock,
                Images = request.Images,
                Status = request.Status,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc,
                ProductId = request.ProductId
            };

            await productDetailRepo.AddAsync(productDetail, cancellationToken);

            var evt = new ProductDetailUpsertedEventV1
            {
                ProductDetailId = productDetail.Id,
                ProductId = productDetail.ProductId,
                UpdatedAtUtc = productDetail.UpdatedAt,
                SchemaVersion = 1
            };

            await outboxRepo.AddAsync(
                OutboxFactory.Create(
                    aggregateType: "ProductDetail",
                    aggregateId: productDetail.Id,
                    eventType: IntegrationEventTypes.ProductDetailUpserted,
                    payload: evt,
                    correlationId: null,
                    partitionKey: productDetail.ProductId.ToString(),
                    schemaVersion: 1),
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

            var productDetail = await productDetailRepo
                .Query()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (productDetail == null)
            {
                return new ServiceResponse { Succeeded = false, Message = "Chi tiết sản phẩm không tồn tại!" };
            }

            var existed = await productDetailRepo.Query()
                .AnyAsync(x => x.Id != id
                            && x.ProductId == request.ProductId
                            && x.Size == request.Size
                            && x.Color == request.Color, cancellationToken);

            if (existed)
            {
                return new ServiceResponse { Succeeded = false, Message = "Chi tiết sản phẩm đã tồn tại!" };
            }

            var nowUtc = DateTime.UtcNow;

            productDetail.Size = request.Size.Trim().ToUpper();
            productDetail.Color = request.Color.Trim();
            productDetail.Stock = request.Stock;
            productDetail.Images = request.Images;
            productDetail.Status = request.Status;
            productDetail.UpdatedAt = nowUtc;

            var evt = new ProductDetailUpsertedEventV1
            {
                ProductDetailId = productDetail.Id,
                ProductId = productDetail.ProductId,
                UpdatedAtUtc = productDetail.UpdatedAt,
                SchemaVersion = 1
            };

            await outboxRepo.AddAsync(
                OutboxFactory.Create(
                    aggregateType: "ProductDetail",
                    aggregateId: productDetail.Id,
                    eventType: IntegrationEventTypes.ProductDetailUpserted,
                    payload: evt,
                    correlationId: null,
                    partitionKey: productDetail.ProductId.ToString(),
                    schemaVersion: 1),
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

            var productDetail = await productDetailRepo
                .Query()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (productDetail == null)
            {
                return new ServiceResponse { Succeeded = false, Message = "Chi tiết sản phẩm không tồn tại!" };
            }

            var deletedAt = DateTime.UtcNow;

            await productDetailRepo.RemoveAsync(productDetail, cancellationToken);

            var evt = new ProductDetailDeletedEventV1
            {
                ProductDetailId = productDetail.Id,
                ProductId = productDetail.ProductId,
                DeletedAtUtc = deletedAt,
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
                    schemaVersion: 1),
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResponse { Succeeded = true, Message = "Xóa chi tiết sản phẩm thành công!" };
        }
    }
}
