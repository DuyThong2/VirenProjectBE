using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Viren.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddMeshyTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "meshy_task",
                columns: table => new
                {
                    meshyTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fitroomTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    remoteTaskId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    progress = table.Column<int>(type: "int", nullable: true),
                    modelGlbUrl = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    modelFbxUrl = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    modelObjUrl = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    modelUsdzUrl = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    thumbnailUrl = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    textureBaseColorUrl = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    textureMetallicUrl = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    textureNormalUrl = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    textureRoughnessUrl = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    errorMessage = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    latestResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    startedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    completedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    lastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meshy_task", x => x.meshyTaskId);
                    table.ForeignKey(
                        name: "FK_meshy_task_fitroom_task_fitroomTaskId",
                        column: x => x.fitroomTaskId,
                        principalTable: "fitroom_task",
                        principalColumn: "fitroomTaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_meshy_task_fitroomTaskId",
                table: "meshy_task",
                column: "fitroomTaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meshy_task_remoteTaskId",
                table: "meshy_task",
                column: "remoteTaskId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "meshy_task");
        }
    }
}
