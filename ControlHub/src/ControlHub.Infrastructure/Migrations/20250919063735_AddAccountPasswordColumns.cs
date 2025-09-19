using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountPasswordColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "Salt",
                table: "Accounts",
                type: "varbinary(64)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "HashPassword",
                table: "Accounts",
                type: "varbinary(64)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "Salt",
                table: "Accounts",
                type: "varbinary(max)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(64)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "HashPassword",
                table: "Accounts",
                type: "varbinary(max)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(64)");
        }
    }
}
