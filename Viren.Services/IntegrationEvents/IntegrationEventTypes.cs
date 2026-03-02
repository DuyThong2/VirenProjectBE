using System.Text.Json.Serialization;

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
        [JsonPropertyName("productId")]
        public Guid ProductId { get; init; }

        [JsonPropertyName("productDetailId")]
        public Guid ProductDetailId { get; init; }

        [JsonPropertyName("categoryId")]
        public Guid CategoryId { get; init; }

        [JsonPropertyName("categoryName")]
        public string CategoryName { get; init; } = "";

        [JsonPropertyName("productName")]
        public string ProductName { get; init; } = "";

        [JsonPropertyName("productDescription")]
        public string ProductDescription { get; init; } = "";

        [JsonPropertyName("size")]
        public string Size { get; init; } = "";

        [JsonPropertyName("colorFamily")]
        public string ColorFamily { get; init; } = "";

        [JsonPropertyName("colorRaw")]
        public string ColorRaw { get; init; } = "";

        [JsonPropertyName("price")]
        public decimal Price { get; init; }

        [JsonPropertyName("stock")]
        public int Stock { get; init; }

        [JsonPropertyName("status")]
        public string Status { get; init; } = "";

        [JsonPropertyName("isSale")]
        public bool IsSale { get; init; }

        // dùng để embedding
        [JsonPropertyName("text")]
        public string Text { get; init; } = "";

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAtUtc { get; init; }

        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; init; } = 1;


        [JsonPropertyName("productDetailImage")]
        public string ? ProductDetailImage { get; set;  } = "";
    }

    public sealed class ProductDetailDeletedEventV1
    {
        [JsonPropertyName("productId")]
        public Guid ProductId { get; init; }

        [JsonPropertyName("productDetailId")]
        public Guid ProductDetailId { get; init; }

        [JsonPropertyName("deletedAt")]
        public DateTime DeletedAtUtc { get; init; }

        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; init; } = 1;
    }
}
