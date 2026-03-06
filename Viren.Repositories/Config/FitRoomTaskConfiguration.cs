using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viren.Repositories.Domains;

namespace Viren.Repositories.Config;

public sealed class FitRoomTaskConfiguration : IEntityTypeConfiguration<FitRoomTask>
{
    public void Configure(EntityTypeBuilder<FitRoomTask> builder)
    {
        builder.ToTable("fitroom_task");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("fitroomTaskId");

        builder.Property(x => x.FitRoomTaskId)
            .HasColumnName("remoteTaskId")
            .HasColumnType("nvarchar(100)")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("userId");

        builder.Property(x => x.ClothType)
            .HasColumnName("clothType")
            .HasColumnType("nvarchar(50)")
            .IsRequired();

        builder.Property(x => x.HdMode)
            .HasColumnName("hdMode");

        builder.Property(x => x.ModelImageKey)
            .HasColumnName("modelImageKey")
            .HasColumnType("nvarchar(500)")
            .IsRequired();

        builder.Property(x => x.ModelImageUrl)
            .HasColumnName("modelImageUrl")
            .HasColumnType("nvarchar(1000)")
            .IsRequired();

        builder.Property(x => x.ClothImageKey)
            .HasColumnName("clothImageKey")
            .HasColumnType("nvarchar(500)")
            .IsRequired();

        builder.Property(x => x.ClothImageUrl)
            .HasColumnName("clothImageUrl")
            .HasColumnType("nvarchar(1000)")
            .IsRequired();

        builder.Property(x => x.LowerClothImageKey)
            .HasColumnName("lowerClothImageKey")
            .HasColumnType("nvarchar(500)");

        builder.Property(x => x.LowerClothImageUrl)
            .HasColumnName("lowerClothImageUrl")
            .HasColumnType("nvarchar(1000)");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("nvarchar(50)")
            .IsRequired();

        builder.Property(x => x.Progress)
            .HasColumnName("progress");

        builder.Property(x => x.ResultUrl)
            .HasColumnName("resultUrl")
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

        builder.HasIndex(x => x.FitRoomTaskId)
            .IsUnique()
            .HasDatabaseName("IX_fitroom_task_remoteTaskId");

        builder.HasIndex(x => new { x.UserId, x.CreatedAt })
            .HasDatabaseName("IX_fitroom_task_userId_createdAt");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
