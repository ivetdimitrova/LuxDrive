using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuxDrive.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "Files",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Files",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Files");
        }
    }
}
