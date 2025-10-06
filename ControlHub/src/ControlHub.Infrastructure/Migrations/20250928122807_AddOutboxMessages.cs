using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TokenEntity_Accounts_AccountId",
                table: "TokenEntity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TokenEntity",
                table: "TokenEntity");

            migrationBuilder.RenameTable(
                name: "TokenEntity",
                newName: "Tokens");

            migrationBuilder.RenameIndex(
                name: "IX_TokenEntity_AccountId",
                table: "Tokens",
                newName: "IX_Tokens_AccountId");

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "Tokens",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Tokens",
                type: "int",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tokens",
                table: "Tokens",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<int>(type: "int", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Processed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_ExpiredAt",
                table: "Tokens",
                column: "ExpiredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Value",
                table: "Tokens",
                column: "Value",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tokens_Accounts_AccountId",
                table: "Tokens",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tokens_Accounts_AccountId",
                table: "Tokens");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tokens",
                table: "Tokens");

            migrationBuilder.DropIndex(
                name: "IX_Tokens_ExpiredAt",
                table: "Tokens");

            migrationBuilder.DropIndex(
                name: "IX_Tokens_Value",
                table: "Tokens");

            migrationBuilder.RenameTable(
                name: "Tokens",
                newName: "TokenEntity");

            migrationBuilder.RenameIndex(
                name: "IX_Tokens_AccountId",
                table: "TokenEntity",
                newName: "IX_TokenEntity_AccountId");

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "TokenEntity",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "TokenEntity",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 50);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TokenEntity",
                table: "TokenEntity",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TokenEntity_Accounts_AccountId",
                table: "TokenEntity",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
