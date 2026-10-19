using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class SeedCatalogImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fill only empty seed images so an uploaded picture is left alone.
            migrationBuilder.Sql("""
                UPDATE Activities SET ImageUrl = '/media/00000000000000000000000000ac1001.png' WHERE Id = 1 AND ImageUrl IS NULL;
                UPDATE Activities SET ImageUrl = '/media/00000000000000000000000000ac1002.png' WHERE Id = 2 AND ImageUrl IS NULL;
                UPDATE Activities SET ImageUrl = '/media/00000000000000000000000000ac1003.png' WHERE Id = 3 AND ImageUrl IS NULL;
                UPDATE Activities SET ImageUrl = '/media/00000000000000000000000000ac1004.png' WHERE Id = 4 AND ImageUrl IS NULL;
                UPDATE Activities SET ImageUrl = '/media/00000000000000000000000000ac1005.png' WHERE Id = 5 AND ImageUrl IS NULL;
                UPDATE Activities SET ImageUrl = '/media/00000000000000000000000000ac1006.png' WHERE Id = 6 AND ImageUrl IS NULL;
                UPDATE Activities SET ImageUrl = '/media/00000000000000000000000000ac1007.png' WHERE Id = 7 AND ImageUrl IS NULL;
                UPDATE Activities SET ImageUrl = '/media/00000000000000000000000000ac1008.png' WHERE Id = 8 AND ImageUrl IS NULL;
                UPDATE Activities SET ImageUrl = '/media/00000000000000000000000000ac1009.png' WHERE Id = 9 AND ImageUrl IS NULL;
                UPDATE Activities SET ImageUrl = '/media/00000000000000000000000000ac1010.png' WHERE Id = 10 AND ImageUrl IS NULL;

                UPDATE Animals SET ImageUrl = '/media/00000000000000000000000000a11001.png' WHERE Id = 1 AND ImageUrl IS NULL;
                UPDATE Animals SET ImageUrl = '/media/00000000000000000000000000a11002.png' WHERE Id = 2 AND ImageUrl IS NULL;
                UPDATE Animals SET ImageUrl = '/media/00000000000000000000000000a11003.png' WHERE Id = 3 AND ImageUrl IS NULL;
                UPDATE Animals SET ImageUrl = '/media/00000000000000000000000000a11004.png' WHERE Id = 4 AND ImageUrl IS NULL;
                UPDATE Animals SET ImageUrl = '/media/00000000000000000000000000a11005.png' WHERE Id = 5 AND ImageUrl IS NULL;
                UPDATE Animals SET ImageUrl = '/media/00000000000000000000000000a11006.png' WHERE Id = 6 AND ImageUrl IS NULL;
                UPDATE Animals SET ImageUrl = '/media/00000000000000000000000000a11007.png' WHERE Id = 7 AND ImageUrl IS NULL;

                UPDATE ZooPartners
                SET Description = COALESCE(Description, N'A small sanctuary in Costa Rica that protects macaws and other forest birds.'),
                    ImageUrl = COALESCE(ImageUrl, N'/media/00000000000000000000000000b01001.png')
                WHERE Id = 1;
                UPDATE ZooPartners
                SET Description = COALESCE(Description, N'A mountain rescue centre in Nepal that cares for injured snow leopards and red pandas.'),
                    ImageUrl = COALESCE(ImageUrl, N'/media/00000000000000000000000000b01002.png')
                WHERE Id = 2;
                UPDATE ZooPartners
                SET Description = COALESCE(Description, N'A coastal wildlife group in Australia that restores habitat for shore birds and reptiles.'),
                    ImageUrl = COALESCE(ImageUrl, N'/media/00000000000000000000000000b01003.png')
                WHERE Id = 3;

                IF NOT EXISTS (SELECT 1 FROM Animals WHERE Id = 8 OR Name = N'Tiko')
                BEGIN
                    SET IDENTITY_INSERT Animals ON;
                    INSERT INTO Animals (Id, AdoptionPrice, CategoryId, CreatedAt, Description, Diet, Habitat, ImageUrl, IsActive, Lifespan, Name, Species, Tag, Weight)
                    VALUES (8, 12.00, 3, '2026-03-09T00:00:00', N'Tiko is a steady Aldabra giant tortoise who spends his mornings grazing and his afternoons in a warm wallow.', N'Herbivore', N'Coastal', N'/media/00000000000000000000000000a11008.png', 1, N'80-120 years', N'Tiko', N'Aldabra Giant Tortoise', 3, 180);
                    SET IDENTITY_INSERT Animals OFF;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.UpdateData(
                table: "Activities",
                keyColumn: "Id",
                keyValue: 1,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Activities",
                keyColumn: "Id",
                keyValue: 2,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Activities",
                keyColumn: "Id",
                keyValue: 3,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Activities",
                keyColumn: "Id",
                keyValue: 4,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Activities",
                keyColumn: "Id",
                keyValue: 5,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Activities",
                keyColumn: "Id",
                keyValue: 6,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Activities",
                keyColumn: "Id",
                keyValue: 7,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Activities",
                keyColumn: "Id",
                keyValue: 8,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Activities",
                keyColumn: "Id",
                keyValue: 9,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Activities",
                keyColumn: "Id",
                keyValue: 10,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 1,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 2,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 3,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 4,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 5,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 6,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Animals",
                keyColumn: "Id",
                keyValue: 7,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "ZooPartners",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "ImageUrl" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ZooPartners",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "ImageUrl" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ZooPartners",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "ImageUrl" },
                values: new object[] { null, null });
        }
    }
}
