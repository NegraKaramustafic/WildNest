using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueBadgeAndZooPartnerNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ZooPartners_Name",
                table: "ZooPartners",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Badges_Name",
                table: "Badges",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ZooPartners_Name",
                table: "ZooPartners");

            migrationBuilder.DropIndex(
                name: "IX_Badges_Name",
                table: "Badges");
        }
    }
}
