namespace Viren.Services.IntegrationEvents
{
    public static class IntegrationEventTypes
    {
        public const string ProductDetailUpserted = "ProductDetail.Upserted";
        public const string ProductDetailDeleted = "ProductDetail.Deleted";

        public const string ProductUpserted = "Product.Upserted";
        public const string CategoryUpserted = "Category.Upserted";
    }

    public sealed class ProductDetailUpsertedEventV1
    {
        public Guid ProductId { get; init; }
        public Guid ProductDetailId { get; init; }

        public Guid CategoryId { get; init; }
        public string CategoryName { get; init; } = default!;

        public string Brand { get; init; } = default!;

        public string Size { get; init; } = default!;

        // phân loại màu (để filter/facet)
        public string ColorFamily { get; init; } = default!;
        public string ColorRaw { get; init; } = default!;

        public decimal Price { get; init; }
        public int Stock { get; init; }

        public string Status { get; init; } = default!;
        public bool IsSale { get; init; }

        public DateTime UpdatedAtUtc { get; init; }

        public int SchemaVersion { get; init; } = 1;
    }

    public sealed class ProductDetailDeletedEventV1
    {
        public Guid ProductId { get; init; }
        public Guid ProductDetailId { get; init; }
        public DateTime DeletedAtUtc { get; init; }
        public int SchemaVersion { get; init; } = 1;
    }
}
