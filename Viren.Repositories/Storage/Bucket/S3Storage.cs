using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Viren.Repositories.Storage.Settings;

namespace Viren.Repositories.Storage.Bucket
{
    public sealed class S3Storage : IS3Storage
    {
        private readonly IAmazonS3 _s3;
        private readonly AwsOptions _opt;

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
