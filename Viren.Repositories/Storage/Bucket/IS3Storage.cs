using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viren.Repositories.Storage.Bucket
{
    public interface IS3Storage
    {
        string PublicBaseUrl { get; }
        Task<IReadOnlyList<UploadedFileDto>> UploadAsync(
            IEnumerable<IFormFile> files,
            CancellationToken ct = default);

        Task<FileStreamResult> DownloadAsync(string key, CancellationToken ct = default);

        Task<IReadOnlyList<string>> Upload3DAsync(
            string url,
            CancellationToken ct = default);
    }
}
