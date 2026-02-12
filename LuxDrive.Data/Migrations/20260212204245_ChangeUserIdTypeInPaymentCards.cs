using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuxDrive.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeUserIdTypeInPaymentCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "PaymentCards",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentCards_UserId",
                table: "PaymentCards",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentCards_AspNetUsers_UserId",
                table: "PaymentCards",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentCards_AspNetUsers_UserId",
                table: "PaymentCards");

            migrationBuilder.DropIndex(
                name: "IX_PaymentCards_UserId",
                table: "PaymentCards");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "PaymentCards",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }
    }
}
