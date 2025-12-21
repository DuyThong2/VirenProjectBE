using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Viren.Repositories.Storage.Bucket
{
    public static class S3KeyBuilder
    {
        private static readonly HashSet<string> ImageExts = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".svg", ".heic" };

        public static string BuildKey(IFormFile file, string prefix)
        {
            var ext = Path.GetExtension(file.FileName);
            var isImage = IsImage(file, ext);
            var folder = isImage ? "images" : "files";

            var name = Path.GetFileNameWithoutExtension(file.FileName);
            var safeName = Regex.Replace(name, @"[^a-zA-Z0-9_\.-]+", "_");

            var fileName = $"{Guid.NewGuid():N}_{safeName}{ext}";

            var basePrefix = string.IsNullOrWhiteSpace(prefix) ? "uploads" : prefix.Trim('/');
            return $"{basePrefix}/{folder}/{fileName}";
        }

        private static bool IsImage(IFormFile file, string ext)
        {
            return (file.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ?? false)
                   || ImageExts.Contains(ext);
        }
    }

}
