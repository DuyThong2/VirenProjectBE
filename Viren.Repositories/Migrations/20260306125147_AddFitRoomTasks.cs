using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Viren.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddFitRoomTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fitroom_task",
                columns: table => new
                {
                    fitroomTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    remoteTaskId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    userId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    clothType = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    hdMode = table.Column<bool>(type: "bit", nullable: false),
                    modelImageKey = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    modelImageUrl = table.Column<string>(type: "nvarchar(1000)", nullable: false),
                    clothImageKey = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    clothImageUrl = table.Column<string>(type: "nvarchar(1000)", nullable: false),
                    lowerClothImageKey = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    lowerClothImageUrl = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    progress = table.Column<int>(type: "int", nullable: true),
                    resultUrl = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    errorMessage = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    latestResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    startedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    completedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    lastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fitroom_task", x => x.fitroomTaskId);
                    table.ForeignKey(
                        name: "FK_fitroom_task_user_userId",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fitroom_task_remoteTaskId",
                table: "fitroom_task",
                column: "remoteTaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fitroom_task_userId_createdAt",
                table: "fitroom_task",
                columns: new[] { "userId", "createdAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fitroom_task");
        }
    }
}
