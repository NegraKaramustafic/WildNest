using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class AddAdoptionValidUntil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ValidUntil",
                table: "Adoptions",
                type: "date",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [Adoptions]
                SET [ValidUntil] = DATEADD(month, 1, CAST(SYSUTCDATETIME() AS date))
                WHERE [Status] = 0 AND [ValidUntil] IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValidUntil",
                table: "Adoptions");
        }
    }
}
