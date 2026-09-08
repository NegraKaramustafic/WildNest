using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimalAndAnimalCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnimalCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Animals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Species = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Habitat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Diet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Lifespan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Tag = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AdoptionPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Animals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Animals_AnimalCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "AnimalCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AnimalCategories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Mammals" },
                    { 2, "Birds" },
                    { 3, "Reptiles" },
                    { 4, "Aquatic" }
                });

            migrationBuilder.InsertData(
                table: "Animals",
                columns: new[] { "Id", "AdoptionPrice", "CategoryId", "CreatedAt", "Description", "Diet", "Habitat", "ImageUrl", "IsActive", "Lifespan", "Name", "Species", "Tag", "Weight" },
                values: new object[,]
                {
                    { 1, null, 1, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "A gentle Asian elephant known for her intelligence and close bond with keepers.", "Herbivore", "Savanna", null, true, "60-70 years", "Kavi", "Asian Elephant", 1, 5400m },
                    { 2, 25.00m, 1, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "A curious red panda who spends most of the day napping in the treetops.", "Omnivore", "Temperate forest", null, true, "8-12 years", "Zara", "Red Panda", 3, 5.0m },
                    { 3, null, 1, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "A confident lioness and one of the most popular residents of the big-cat habitat.", "Carnivore", "Savanna", null, true, "10-14 years", "Nala", "African Lion", 1, 185m },
                    { 4, null, 2, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "A brightly colored macaw who greets visitors with an impressive vocal range.", "Omnivore", "Rainforest canopy", null, true, "40-50 years", "Pico", "Scarlet Macaw", 2, 1.2m },
                    { 5, 40.00m, 3, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "A rescued green sea turtle now thriving in the aquatic conservation exhibit.", "Herbivore", "Coastal waters", null, true, "80+ years", "Kai", "Green Sea Turtle", 3, 160m },
                    { 6, null, 1, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "An elusive snow leopard with a thick spotted coat, often seen at dusk.", "Carnivore", "Alpine mountains", null, true, "15-18 years", "Luna", "Snow Leopard", 1, 50m },
                    { 7, null, 2, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "An energetic Humboldt penguin who loves swimming laps during feeding time.", "Carnivore", "Coastal islands", null, true, "15-20 years", "Finn", "Humboldt Penguin", 2, 4.5m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Animals_CategoryId",
                table: "Animals",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Animals");

            migrationBuilder.DropTable(
                name: "AnimalCategories");
        }
    }
}
