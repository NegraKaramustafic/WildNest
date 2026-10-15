using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewSentiment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SentimentLabel",
                table: "Reviews",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SentimentScore",
                table: "Reviews",
                type: "float",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "SentimentLabel", "SentimentScore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "SentimentLabel", "SentimentScore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "SentimentLabel", "SentimentScore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "SentimentLabel", "SentimentScore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "SentimentLabel", "SentimentScore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "SentimentLabel", "SentimentScore" },
                values: new object[] { null, null });

            migrationBuilder.InsertData(
                table: "Reviews",
                columns: new[] { "Id", "ActivityId", "Comment", "CreatedAt", "Rating", "SentimentLabel", "SentimentScore", "UserId" },
                values: new object[,]
                {
                    { 7, 3, "Terrible experience. The guide was rude and we waited far too long.", new DateTime(2025, 10, 12, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, 3 },
                    { 8, 3, "The session starts at 10:00 and lasts about one hour.", new DateTime(2025, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), 3, null, null, 4 },
                    { 9, 3, "Wonderful morning. The keepers were friendly and I would recommend this activity.", new DateTime(2025, 12, 16, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, null, 5 },
                    { 10, 4, "Awful organisation. The enclosure was dirty and the whole visit felt wasted.", new DateTime(2026, 1, 8, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, 3 },
                    { 11, 4, "Parking is beside the main gate and tickets are checked at the entrance.", new DateTime(2026, 2, 11, 0, 0, 0, 0, DateTimeKind.Utc), 3, null, null, 4 },
                    { 12, 4, "Excellent activity. Everything was well organised and the kids were thrilled.", new DateTime(2026, 4, 3, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, null, 5 },
                    { 13, 6, "Disappointing tour. The staff were unhelpful and this was the worst workshop.", new DateTime(2025, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, 4 },
                    { 14, 6, "The workshop is listed on the Tuesday schedule and lasts ninety minutes.", new DateTime(2026, 1, 19, 0, 0, 0, 0, DateTimeKind.Utc), 3, null, null, 5 },
                    { 15, 5, "We loved this safari. It was amazing and I recommend it to every family.", new DateTime(2026, 2, 20, 0, 0, 0, 0, DateTimeKind.Utc), 4, null, null, 3 },
                    { 16, 7, "The worst feeding we have had. Staff were rude and the delay was awful.", new DateTime(2026, 3, 2, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, 5 },
                    { 17, 7, "Tickets are checked at the entrance and the tour starts at the main gate.", new DateTime(2026, 4, 9, 0, 0, 0, 0, DateTimeKind.Utc), 3, null, null, 4 },
                    { 18, 6, "A wonderful workshop. The guide was excellent and we loved every minute.", new DateTime(2026, 4, 15, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, null, 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DropColumn(
                name: "SentimentLabel",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "SentimentScore",
                table: "Reviews");
        }
    }
}
