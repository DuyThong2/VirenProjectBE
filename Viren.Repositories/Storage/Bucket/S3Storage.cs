using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Buffers;
using Viren.Repositories.Storage.Settings;

namespace Viren.Repositories.Storage.Bucket
{
    public sealed class S3Storage : IS3Storage
    {
        private readonly IAmazonS3 _s3;
        private readonly AwsOptions _opt;

        public string PublicBaseUrl => _opt.PublicBaseUrl ?? string.Empty;

        public S3Storage(IAmazonS3 s3, IOptions<AwsOptions> opt)
        {
            _s3 = s3;
            _opt = opt.Value;
        }

        public async Task<IReadOnlyList<UploadedFileDto>> UploadAsync(
            IEnumerable<IFormFile> files, CancellationToken ct = default)
        {
            var results = new List<UploadedFileDto>();

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                // Tạo key an toàn: prefix/yyyy/MM/dd/{guid}{ext}
                var key = S3KeyBuilder.BuildKey(file, _opt.KeyPrefix);

                using var stream = file.OpenReadStream();
                var put = new PutObjectRequest
                {
                    BucketName = _opt.Bucket,
                    Key = key,
                    InputStream = stream,
                    ContentType = file.ContentType,
                    // ACL = S3CannedACL.PublicRead, // chỉ bật nếu bucket public
                    Metadata =
                    {
                        ["x-amz-meta-original-name"] = file.FileName
                    }
                };

                _ = await _s3.PutObjectAsync(put, ct);

                // 1) URL ổn định (để lưu DB & hiển thị lâu dài)
                // - Nếu có CDN/public: https://cdn.example.com/{key}
                // - Nếu private bucket: fallback API download /api/claim-uploads/{key}
                var stableUrl = !string.IsNullOrWhiteSpace(_opt.PublicBaseUrl)
                    ? $"{_opt.PublicBaseUrl!.TrimEnd('/')}/{key}"
                    : $"/api/claim-uploads/{key}";

                // 2) PreviewUrl (presigned, hết hạn sau 30')
                var previewUrl = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
                {
                    BucketName = _opt.Bucket,
                    Key = key,
                    Expires = DateTime.UtcNow.AddMinutes(30)
                });

                results.Add(new UploadedFileDto
                {
                    Key = key,
                    ContentType = file.ContentType,
                    Size = file.Length,
                    Url = stableUrl,
                    PreviewUrl = previewUrl,
                    OriginalFileName = file.FileName,          
                    UploadedAtUtc = DateTime.UtcNow
                });
            }

            return results;
        }

        public async Task<FileStreamResult> DownloadAsync(string key, CancellationToken ct = default)
        {
            var resp = await _s3.GetObjectAsync(_opt.Bucket, key, ct);
            return new FileStreamResult(resp.ResponseStream, resp.Headers.ContentType)
            {
                FileDownloadName = Path.GetFileName(key)
            };
        }

        public async Task<IReadOnlyList<string>> Upload3DAsync(string url, CancellationToken ct = default)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(120);

            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead, ct);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType
                              ?? "model/gltf-binary";

            var originalFileName = Path.GetFileName(new Uri(url).AbsolutePath);
            if (string.IsNullOrWhiteSpace(originalFileName))
                originalFileName = $"{Guid.NewGuid()}.glb";

            var ext = Path.GetExtension(originalFileName);
            var now = DateTime.UtcNow;
            var key = string.IsNullOrWhiteSpace(_opt.KeyPrefix)
                ? $"3d/{now:yyyy/MM/dd}/{Guid.NewGuid()}{ext}"
                : $"{_opt.KeyPrefix.TrimEnd('/')}/3d/{now:yyyy/MM/dd}/{Guid.NewGuid()}{ext}";

            // Đọc toàn bộ content vào byte[] (AWS S3 SDK cần biết Content-Length)
            var rawBytes = await response.Content.ReadAsByteArrayAsync(ct);
            var fileSize = rawBytes.Length;

            // Mượn buffer từ ArrayPool thay vì cấp phát mới — tái sử dụng RAM giữa các request
            var rentedBuffer = ArrayPool<byte>.Shared.Rent(fileSize);
            try
            {
                Buffer.BlockCopy(rawBytes, 0, rentedBuffer, 0, fileSize);
                rawBytes = null; // bỏ tham chiếu sớm, GC có thể thu hồi ngay

                using var memStream = new MemoryStream(
                    rentedBuffer, 0, fileSize,
                    writable: false,
                    publiclyVisible: false);

                var put = new PutObjectRequest
                {
                    BucketName = _opt.Bucket,
                    Key = key,
                    InputStream = memStream,
                    ContentType = contentType,
                    Headers = { ContentLength = fileSize },
                    Metadata =
                    {
                        ["x-amz-meta-original-name"] = originalFileName,
                        ["x-amz-meta-source-url"]    = url[..Math.Min(url.Length, 512)]
                    }
                };

                _ = await _s3.PutObjectAsync(put, ct);
            }
            finally
            {
                // Trả buffer về pool và zero-fill để xóa data cũ
                ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: true);
            }

            var stableUrl = !string.IsNullOrWhiteSpace(_opt.PublicBaseUrl)
                ? $"{_opt.PublicBaseUrl!.TrimEnd('/')}/{key}"
                : $"/api/claim-uploads/{key}";

            return new List<string> { stableUrl }.AsReadOnly();
        }
    }

    public class UploadedFileDto
    {
        public string Key { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public long Size { get; set; }
        public string Url { get; set; } = default!;
        public string PreviewUrl { get; set; } = default!;

        public string OriginalFileName { get; set; } = default!;
        public DateTime UploadedAtUtc { get; set; }
    }
}
