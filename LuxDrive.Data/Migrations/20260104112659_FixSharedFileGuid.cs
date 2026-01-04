using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuxDrive.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixSharedFileGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SharedFiles_ReceiverId",
                table: "SharedFiles",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedFiles_SenderId",
                table: "SharedFiles",
                column: "SenderId");

            migrationBuilder.AddForeignKey(
                name: "FK_SharedFiles_AspNetUsers_ReceiverId",
                table: "SharedFiles",
                column: "ReceiverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SharedFiles_AspNetUsers_SenderId",
                table: "SharedFiles",
                column: "SenderId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SharedFiles_AspNetUsers_ReceiverId",
                table: "SharedFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_SharedFiles_AspNetUsers_SenderId",
                table: "SharedFiles");

            migrationBuilder.DropIndex(
                name: "IX_SharedFiles_ReceiverId",
                table: "SharedFiles");

            migrationBuilder.DropIndex(
                name: "IX_SharedFiles_SenderId",
                table: "SharedFiles");
        }
    }
}
