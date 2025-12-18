using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Viren.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class updateUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "user",
                type: "nvarchar(max)",
                nullable: true);

            /*migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BirthDate",
                table: "user",
                type: "datetimeoffset",
                nullable: true);*/

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "user",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "user",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "user");

            /*migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "user");*/

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "user");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "user");
        }
    }
}
