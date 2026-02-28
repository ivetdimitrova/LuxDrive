using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuxDrive.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnDescriptionsToAllTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "FriendId",
                table: "UserFriends",
                type: "uniqueidentifier",
                nullable: false,
                comment: "Identifier of the related friend user",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "UserFriends",
                type: "uniqueidentifier",
                nullable: false,
                comment: "Identifier of the user in the friendship relationship",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SharedOn",
                table: "SharedFiles",
                type: "datetime2",
                nullable: false,
                comment: "UTC date and time when the file was shared",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<Guid>(
                name: "SenderId",
                table: "SharedFiles",
                type: "uniqueidentifier",
                nullable: false,
                comment: "Identifier of the user who shared the file",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "ReceiverId",
                table: "SharedFiles",
                type: "uniqueidentifier",
                nullable: false,
                comment: "Identifier of the user who received the shared file",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "FileId",
                table: "SharedFiles",
                type: "uniqueidentifier",
                nullable: false,
                comment: "Identifier of the file being shared",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SharedFiles",
                type: "uniqueidentifier",
                nullable: false,
                comment: "Unique identifier for the file share record",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "PaymentCards",
                type: "uniqueidentifier",
                nullable: false,
                comment: "Identifier of the user who owns the payment card",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "CardType",
                table: "PaymentCards",
                type: "nvarchar(max)",
                nullable: false,
                comment: "Type of the payment card (e.g., Visa, MasterCard)",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CardLast4",
                table: "PaymentCards",
                type: "nvarchar(max)",
                nullable: false,
                comment: "Last four digits of the payment card number",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "PaymentCards",
                type: "uniqueidentifier",
                nullable: false,
                comment: "Unique identifier for the payment card record",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "FriendRequests",
                type: "int",
                nullable: false,
                comment: "Current status of the friend request (Pending, Accepted, Rejected)",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<Guid>(
                name: "SenderId",
                table: "FriendRequests",
                type: "uniqueidentifier",
                nullable: false,
                comment: "Identifier of the user who sent the friend request",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "ReceiverId",
                table: "FriendRequests",
                type: "uniqueidentifier",
                nullable: false,
                comment: "Identifier of the user who received the friend request",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedOn",
                table: "FriendRequests",
                type: "datetime2",
                nullable: false,
                comment: "UTC date and time when the friend request was created",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "FriendRequests",
                type: "uniqueidentifier",
                nullable: false,
                comment: "Unique identifier for the friend request",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "FriendId",
                table: "UserFriends",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "Identifier of the related friend user");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "UserFriends",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "Identifier of the user in the friendship relationship");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SharedOn",
                table: "SharedFiles",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "UTC date and time when the file was shared");

            migrationBuilder.AlterColumn<Guid>(
                name: "SenderId",
                table: "SharedFiles",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "Identifier of the user who shared the file");

            migrationBuilder.AlterColumn<Guid>(
                name: "ReceiverId",
                table: "SharedFiles",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "Identifier of the user who received the shared file");

            migrationBuilder.AlterColumn<Guid>(
                name: "FileId",
                table: "SharedFiles",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "Identifier of the file being shared");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SharedFiles",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "Unique identifier for the file share record");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "PaymentCards",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "Identifier of the user who owns the payment card");

            migrationBuilder.AlterColumn<string>(
                name: "CardType",
                table: "PaymentCards",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldComment: "Type of the payment card (e.g., Visa, MasterCard)");

            migrationBuilder.AlterColumn<string>(
                name: "CardLast4",
                table: "PaymentCards",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldComment: "Last four digits of the payment card number");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "PaymentCards",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "Unique identifier for the payment card record");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "FriendRequests",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Current status of the friend request (Pending, Accepted, Rejected)");

            migrationBuilder.AlterColumn<Guid>(
                name: "SenderId",
                table: "FriendRequests",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "Identifier of the user who sent the friend request");

            migrationBuilder.AlterColumn<Guid>(
                name: "ReceiverId",
                table: "FriendRequests",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "Identifier of the user who received the friend request");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedOn",
                table: "FriendRequests",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "UTC date and time when the friend request was created");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "FriendRequests",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "Unique identifier for the friend request");
        }
    }
}
