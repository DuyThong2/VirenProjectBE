using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Common;

namespace Viren.Repositories.Domains
{
    public sealed class MeshyTask : BaseEntity<Guid>
    {
        public Guid FitRoomTaskId { get; set; }

        public FitRoomTask FitRoomTask { get; set; } = null!;

        public string MeshyTaskId { get; set; } = string.Empty;

        public string Status { get; set; } = "PENDING";

        public int? Progress { get; set; }

        public string? ModelGlbUrl { get; set; }

        public string? ModelFbxUrl { get; set; }

        public string? ModelObjUrl { get; set; }

        public string? ModelUsdzUrl { get; set; }

        public string? ThumbnailUrl { get; set; }

        public string? TextureBaseColorUrl { get; set; }

        public string? TextureMetallicUrl { get; set; }

        public string? TextureNormalUrl { get; set; }

        public string? TextureRoughnessUrl { get; set; }

        public string? ErrorMessage { get; set; }

        public string LatestResponseJson { get; set; } = "{}";

        public DateTime CreatedAt { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime LastSyncedAt { get; set; }
    }
}
