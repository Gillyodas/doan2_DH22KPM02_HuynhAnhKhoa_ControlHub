using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeletedToRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent check for FirstName
            migrationBuilder.Sql(@"
                IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'FirstName' AND Object_ID = Object_ID(N'[ControlHub].[Users]'))
                BEGIN
                    ALTER TABLE [ControlHub].[Users] ADD [FirstName] nvarchar(100) NULL;
                END
            ");

            // Idempotent check for LastName
            migrationBuilder.Sql(@"
                IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'LastName' AND Object_ID = Object_ID(N'[ControlHub].[Users]'))
                BEGIN
                    ALTER TABLE [ControlHub].[Users] ADD [LastName] nvarchar(100) NULL;
                END
            ");

            // Idempotent check for PhoneNumber
            migrationBuilder.Sql(@"
                IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PhoneNumber' AND Object_ID = Object_ID(N'[ControlHub].[Users]'))
                BEGIN
                    ALTER TABLE [ControlHub].[Users] ADD [PhoneNumber] nvarchar(20) NULL;
                END
            ");

            // Idempotent check for IsDeleted in Roles
            migrationBuilder.Sql(@"
                IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'IsDeleted' AND Object_ID = Object_ID(N'[ControlHub].[Roles]'))
                BEGIN
                    ALTER TABLE [ControlHub].[Roles] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS BIT);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "ControlHub",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "ControlHub",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                schema: "ControlHub",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "ControlHub",
                table: "Roles");
        }
    }
}
