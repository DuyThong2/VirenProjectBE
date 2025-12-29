using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Viren.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class InitPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payment_orderId",
                table: "payment");

            migrationBuilder.AlterColumn<string>(
                name: "paymentType",
                table: "payment",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Cod",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)");

            migrationBuilder.AddColumn<Guid>(
                name: "userId",
                table: "payment",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_orderId",
                table: "payment",
                column: "orderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_userId",
                table: "payment",
                column: "userId");

            migrationBuilder.AddForeignKey(
                name: "FK_payment_user_userId",
                table: "payment",
                column: "userId",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payment_user_userId",
                table: "payment");

            migrationBuilder.DropIndex(
                name: "IX_payment_orderId",
                table: "payment");

            migrationBuilder.DropIndex(
                name: "IX_payment_userId",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "userId",
                table: "payment");

            migrationBuilder.AlterColumn<string>(
                name: "paymentType",
                table: "payment",
                type: "nvarchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "Cod");

            migrationBuilder.CreateIndex(
                name: "IX_payment_orderId",
                table: "payment",
                column: "orderId");
        }
    }
}
