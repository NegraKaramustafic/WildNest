using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAnimalSeedFromMockups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AnimalCategories",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Primates");

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AdoptionPrice", "Description", "Habitat", "Tag", "Weight" },
                values: new object[] { 15.00m, "Kavi is a gentle 15-year-old Asian elephant rescued from a logging camp. He loves watermelons and mud baths.", "Tropical", 3, 4500m });

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AdoptionPrice", "Description", "Habitat", "Tag" },
                values: new object[] { null, "Zara is a playful red panda who spends her days napping in the treetops. She is especially fond of bamboo shoots and apple slices.", "Temperate", 1 });

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CategoryId", "Description", "Diet", "Habitat", "Lifespan", "Name", "Species", "Tag", "Weight" },
                values: new object[] { 4, "Milo is a lively ring-tailed lemur who sunbathes with his troop each morning. He never misses a chance to steal a grape.", "Omnivore", "Tropical", "16-19 years", "Milo", "Ring-tailed Lemur", 2, 2.5m });

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "AdoptionPrice", "CategoryId", "Description", "Diet", "Habitat", "Lifespan", "Name", "Species", "Tag", "Weight" },
                values: new object[] { 20.00m, 1, "Luna is a quiet snow leopard often seen stretching on cool rocky ledges at dusk. She is shy with crowds but very curious.", "Carnivore", "Mountain", "15-18 years", "Luna", "Snow Leopard", 3, 40m });

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "AdoptionPrice", "CategoryId", "Description", "Habitat", "Lifespan", "Name", "Species", "Tag", "Weight" },
                values: new object[] { 25.00m, 1, "Bao is a calm giant panda with a famously picky appetite. His keepers know he will do almost anything for fresh bamboo.", "Temperate", "20 years", "Bao", "Giant Panda", 1, 110m });

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CategoryId", "Description", "Diet", "Habitat", "Lifespan", "Name", "Species", "Tag", "Weight" },
                values: new object[] { 2, "Rio is a brightly colored scarlet macaw who greets visitors with an impressive vocal range. He loves mango and showing off.", "Omnivore", "Tropical", "40-50 years", "Rio", "Scarlet Macaw", 2, 1.0m });

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "AdoptionPrice", "CategoryId", "Description", "Habitat", "Lifespan", "Name", "Species", "Tag", "Weight" },
                values: new object[] { 18.00m, 1, "Nala is a confident lioness and one of the most popular residents of the savanna habitat. She spends warm afternoons dozing in the grass.", "Savanna", "10-14 years", "Nala", "African Lion", 1, 160m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AnimalCategories",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Aquatic");

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AdoptionPrice", "Description", "Habitat", "Tag", "Weight" },
                values: new object[] { null, "A gentle Asian elephant known for her intelligence and close bond with keepers.", "Savanna", 1, 5400m });

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AdoptionPrice", "Description", "Habitat", "Tag" },
                values: new object[] { 25.00m, "A curious red panda who spends most of the day napping in the treetops.", "Temperate forest", 3 });

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CategoryId", "Description", "Diet", "Habitat", "Lifespan", "Name", "Species", "Tag", "Weight" },
                values: new object[] { 1, "A confident lioness and one of the most popular residents of the big-cat habitat.", "Carnivore", "Savanna", "10-14 years", "Nala", "African Lion", 1, 185m });

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "AdoptionPrice", "CategoryId", "Description", "Diet", "Habitat", "Lifespan", "Name", "Species", "Tag", "Weight" },
                values: new object[] { null, 2, "A brightly colored macaw who greets visitors with an impressive vocal range.", "Omnivore", "Rainforest canopy", "40-50 years", "Pico", "Scarlet Macaw", 2, 1.2m });

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "AdoptionPrice", "CategoryId", "Description", "Habitat", "Lifespan", "Name", "Species", "Tag", "Weight" },
                values: new object[] { 40.00m, 3, "A rescued green sea turtle now thriving in the aquatic conservation exhibit.", "Coastal waters", "80+ years", "Kai", "Green Sea Turtle", 3, 160m });

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CategoryId", "Description", "Diet", "Habitat", "Lifespan", "Name", "Species", "Tag", "Weight" },
                values: new object[] { 1, "An elusive snow leopard with a thick spotted coat, often seen at dusk.", "Carnivore", "Alpine mountains", "15-18 years", "Luna", "Snow Leopard", 1, 50m });

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "AdoptionPrice", "CategoryId", "Description", "Habitat", "Lifespan", "Name", "Species", "Tag", "Weight" },
                values: new object[] { null, 2, "An energetic Humboldt penguin who loves swimming laps during feeding time.", "Coastal islands", "15-20 years", "Finn", "Humboldt Penguin", 2, 4.5m });
        }
    }
}
