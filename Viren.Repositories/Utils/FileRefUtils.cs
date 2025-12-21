using System.Text.Json;
using Viren.Repositories.Storage.Bucket;
using Viren.Services.Dtos.Requests;

namespace Viren.Repositories.Utils;

    public static class FileRefUtils
    {
        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        /// <summary>
        /// Parse JSON [{key,url}] hoặc fallback JSON ["url1","url2"] hoặc CSV "a,b"
        /// → List&lt;FileRefDto&gt; (Url được strip query)
        /// </summary>
        public static List<FileRefDto> ParseAny(string? jsonOrCsv)
        {
            var list = new List<FileRefDto>();
            if (string.IsNullOrWhiteSpace(jsonOrCsv)) return list;

            try
            {
                var objs = JsonSerializer.Deserialize<List<FileRefDto>>(jsonOrCsv, _json);
                if (objs is not null)
                {
                    foreach (var o in objs)
                    {
                        if (o is null) continue;

                        var key = o.Key?.Trim();
                        var url = StripQuery(o.Url);

                        // nếu cả key & url đều trống thì bỏ
                        if (string.IsNullOrWhiteSpace(key) && string.IsNullOrWhiteSpace(url))
                            continue;

                        list.Add(new FileRefDto
                        {
                            Key = key,
                            Url = url,
                            Name = string.IsNullOrWhiteSpace(o.Name) ? null : o.Name!.Trim(),
                            Size = o.Size,
                            ContentType = string.IsNullOrWhiteSpace(o.ContentType) ? null : o.ContentType!.Trim(),
                            UploadedAtUtc = o.UploadedAtUtc
                        });
                    }
                }
                if (list.Count > 0) return Distinct(list);
            }
            catch { /* ignore */ }

            // 2) JSON of string[] (chỉ có Url)
            try
            {
                var arr = JsonSerializer.Deserialize<List<string>>(jsonOrCsv, _json);
                if (arr is not null)
                {
                    foreach (var u in arr.Where(s => !string.IsNullOrWhiteSpace(s)))
                        list.Add(new FileRefDto { Url = StripQuery(u) });
                }
                if (list.Count > 0) return Distinct(list);
            }
            catch { /* ignore */ }

            // 3) CSV (chỉ có Url)
            foreach (var u in jsonOrCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                list.Add(new FileRefDto { Url = StripQuery(u) });

            return Distinct(list);
        }


        /// <summary>Serialize về JSON chuẩn [{key,url}] để lưu DB</summary>
        public static string ToJson(List<FileRefDto> refs) =>
            JsonSerializer.Serialize(Normalize(refs), _json);

        /// <summary>Chuẩn hóa từng phần tử (trim + strip query)</summary>
        public static List<FileRefDto> Normalize(IEnumerable<FileRefDto> refs)
        {
            var outList = new List<FileRefDto>();
            foreach (var r in refs)
            {
                var key = r.Key?.Trim();
                var url = StripQuery(r.Url);
                if (string.IsNullOrWhiteSpace(key) && string.IsNullOrWhiteSpace(url))
                    continue;

                outList.Add(new FileRefDto
                {
                    Key = key,
                    Url = url,
                    Name = string.IsNullOrWhiteSpace(r.Name) ? null : r.Name!.Trim(),
                    Size = r.Size,
                    ContentType = string.IsNullOrWhiteSpace(r.ContentType) ? null : r.ContentType!.Trim(),
                    UploadedAtUtc = r.UploadedAtUtc
                });
            }
            return outList;
        }


        /// <summary>Bỏ trùng theo Key (ưu tiên), fallback Url (so sánh ignore-case)</summary>
        public static List<FileRefDto> Distinct(IEnumerable<FileRefDto> refs)
        {
            var seenKey = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenUrl = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<FileRefDto>();

            foreach (var r in Normalize(refs))
            {
                if (!string.IsNullOrWhiteSpace(r.Key))
                {
                    if (seenKey.Add(r.Key)) result.Add(r);
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(r.Url))
                {
                    if (seenUrl.Add(r.Url!)) result.Add(r);
                }
            }
            return result;
        }

        /// <summary>
        /// Diff theo Key trước, fallback Url (ignore-case).
        /// Trả về: keep, add, del (để bạn xoá vật lý nếu cần).
        /// </summary>
        public static (List<FileRefDto> keep, List<FileRefDto> add, List<FileRefDto> del)
            DiffByKeyThenUrl(List<FileRefDto> existing, List<FileRefDto> desired)
        {
            bool Same(FileRefDto a, FileRefDto b)
            {
                if (!string.IsNullOrWhiteSpace(a.Key) && !string.IsNullOrWhiteSpace(b.Key))
                    return string.Equals(a.Key, b.Key, StringComparison.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(a.Url) && !string.IsNullOrWhiteSpace(b.Url))
                    return string.Equals(a.Url, b.Url, StringComparison.OrdinalIgnoreCase);
                return false;
            }

            var keep = new List<FileRefDto>();
            var add = new List<FileRefDto>();
            var del = new List<FileRefDto>();

            var ex = Distinct(existing);
            var de = Distinct(desired);

            foreach (var d in de) (ex.Any(e => Same(e, d)) ? keep : add).Add(d);
            foreach (var e in ex) if (!de.Any(d => Same(d, e))) del.Add(e);

            return (keep, add, del);
        }

        /// <summary>Tạo FileRefDto từ kết quả upload S3</summary>
        public static List<FileRefDto> FromUploaded(IEnumerable<UploadedFileDto> uploaded)
        {
            var res = new List<FileRefDto>();
            foreach (var u in uploaded)
            {
                if (string.IsNullOrWhiteSpace(u?.Key)) continue;
                res.Add(new FileRefDto
                {
                    Key = u.Key,
                    Url = StripQuery(u.Url),
                    Name = u.OriginalFileName,          
                    Size = u.Size,                      
                    ContentType = u.ContentType,       
                    UploadedAtUtc = u.UploadedAtUtc    
                });
            }
            return res;
        }

        public static string? StripQuery(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            try
            {
                var s = url.Trim();
                var idx = s.IndexOfAny(new[] { '?', '#' });
                return idx >= 0 ? s[..idx] : s;
            }
            catch { return url; }
        }
    }
