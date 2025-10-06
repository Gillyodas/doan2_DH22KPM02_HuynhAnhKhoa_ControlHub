using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitAccountIdentifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Accounts");

            migrationBuilder.CreateTable(
                name: "AccountIdentifiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    NormalizedValue = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountIdentifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountIdentifiers_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TokenEntity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenEntity_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountIdentifiers_AccountId",
                table: "AccountIdentifiers",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountIdentifiers_Type_NormalizedValue",
                table: "AccountIdentifiers",
                columns: new[] { "Type", "NormalizedValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenEntity_AccountId",
                table: "TokenEntity",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountIdentifiers");

            migrationBuilder.DropTable(
                name: "TokenEntity");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Accounts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
