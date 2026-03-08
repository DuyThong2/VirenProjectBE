using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Domains;

namespace Viren.Repositories.Config
{
    public sealed class MeshyTaskConfiguration : IEntityTypeConfiguration<MeshyTask>
    {
        public void Configure(EntityTypeBuilder<MeshyTask> builder)
        {
            builder.ToTable("meshy_task");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("meshyTaskId");

            builder.Property(x => x.FitRoomTaskId)
                .HasColumnName("fitroomTaskId")
                .IsRequired();

            builder.Property(x => x.MeshyTaskId)
                .HasColumnName("remoteTaskId")
                .HasColumnType("nvarchar(100)")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasColumnType("nvarchar(50)")
                .IsRequired();

            builder.Property(x => x.Progress)
                .HasColumnName("progress");

            builder.Property(x => x.ModelGlbUrl)
                .HasColumnName("modelGlbUrl")
                .HasColumnType("nvarchar(2000)");

            builder.Property(x => x.ModelFbxUrl)
                .HasColumnName("modelFbxUrl")
                .HasColumnType("nvarchar(2000)");

            builder.Property(x => x.ModelObjUrl)
                .HasColumnName("modelObjUrl")
                .HasColumnType("nvarchar(2000)");

            builder.Property(x => x.ModelUsdzUrl)
                .HasColumnName("modelUsdzUrl")
                .HasColumnType("nvarchar(2000)");

            builder.Property(x => x.ThumbnailUrl)
                .HasColumnName("thumbnailUrl")
                .HasColumnType("nvarchar(2000)");

            builder.Property(x => x.TextureBaseColorUrl)
                .HasColumnName("textureBaseColorUrl")
                .HasColumnType("nvarchar(2000)");

            builder.Property(x => x.TextureMetallicUrl)
                .HasColumnName("textureMetallicUrl")
                .HasColumnType("nvarchar(2000)");

            builder.Property(x => x.TextureNormalUrl)
                .HasColumnName("textureNormalUrl")
                .HasColumnType("nvarchar(2000)");

            builder.Property(x => x.TextureRoughnessUrl)
                .HasColumnName("textureRoughnessUrl")
                .HasColumnType("nvarchar(2000)");

            builder.Property(x => x.ErrorMessage)
                .HasColumnName("errorMessage")
                .HasColumnType("nvarchar(2000)");

            builder.Property(x => x.LatestResponseJson)
                .HasColumnName("latestResponseJson")
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnName("createdAt");

            builder.Property(x => x.StartedAt)
                .HasColumnName("startedAt");

            builder.Property(x => x.CompletedAt)
                .HasColumnName("completedAt");

            builder.Property(x => x.LastSyncedAt)
                .HasColumnName("lastSyncedAt");

            builder.HasIndex(x => x.MeshyTaskId)
                .IsUnique()
                .HasDatabaseName("IX_meshy_task_remoteTaskId");

            builder.HasIndex(x => x.FitRoomTaskId)
                .HasDatabaseName("IX_meshy_task_fitroomTaskId");

            builder.HasOne(x => x.FitRoomTask)
                .WithOne()
                .HasForeignKey<MeshyTask>(x => x.FitRoomTaskId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
