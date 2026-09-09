using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class AddReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Reviews",
                columns: new[] { "Id", "ActivityId", "Comment", "CreatedAt", "Rating", "UserId" },
                values: new object[,]
                {
                    { 1, 1, "Amazing experience! The kids were thrilled, staff were very kind.", new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), 5, 3 },
                    { 2, 1, "Kavi is such a gentle giant. Helping with the feeding was unforgettable.", new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), 5, 4 },
                    { 3, 1, "Really well organised. We learned a lot and the keepers were so patient.", new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), 4, 5 },
                    { 4, 2, "Magical evening walk. Spotting the nocturnal animals was a highlight of our trip.", new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), 5, 3 },
                    { 5, 2, "Loved the atmosphere after dark. Guides were knowledgeable and made everyone feel safe.", new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), 4, 4 },
                    { 6, 2, "A must-do! Quiet, atmospheric, and the kids stayed engaged the whole time.", new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), 5, 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ActivityId",
                table: "Reviews",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId_ActivityId",
                table: "Reviews",
                columns: new[] { "UserId", "ActivityId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reviews");
        }
    }
}
