using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NewsItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AuthorUserId = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsItems_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "NewsItems",
                columns: new[] { "Id", "AuthorUserId", "CreatedAt", "ImageUrl", "IsPublished", "PublishedAt", "Text", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 10, 12, 18, 0, 0, 0, DateTimeKind.Utc), "/media/00000000000000000000000000a11e01.png", true, new DateTime(2026, 10, 12, 18, 0, 0, 0, DateTimeKind.Utc), "After-dark walks with the night animals are open again this weekend. Places are limited, so reserve a schedule in the app before you arrive.", "Night safari walks return this weekend", null },
                    { 2, 1, new DateTime(2026, 9, 28, 10, 30, 0, 0, DateTimeKind.Utc), "/media/00000000000000000000000000a11e02.png", true, new DateTime(2026, 9, 28, 10, 30, 0, 0, DateTimeKind.Utc), "Two red panda twins are now on view in the forest habitat. Keep a little distance, and follow the keeper's notes posted by the enclosure.", "Meet the new red panda twins", null },
                    { 3, 1, new DateTime(2026, 4, 2, 9, 0, 0, 0, DateTimeKind.Utc), "/media/00000000000000000000000000a11e03.png", true, new DateTime(2026, 4, 2, 9, 0, 0, 0, DateTimeKind.Utc), "The spring planting in the wetland habitat is finished. New shade and water plants are in place for the warmer months.", "Spring habitat renewal is complete", null },
                    { 4, 1, new DateTime(2026, 10, 19, 8, 0, 0, 0, DateTimeKind.Utc), "/media/00000000000000000000000000a11e04.png", false, new DateTime(2026, 10, 19, 8, 0, 0, 0, DateTimeKind.Utc), "A draft outline for the winter lectures. This item stays unpublished until the dates are confirmed.", "Winter lecture series outline", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsItems_AuthorUserId",
                table: "NewsItems",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsItems_IsPublished_PublishedAt",
                table: "NewsItems",
                columns: new[] { "IsPublished", "PublishedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsItems");
        }
    }
}
