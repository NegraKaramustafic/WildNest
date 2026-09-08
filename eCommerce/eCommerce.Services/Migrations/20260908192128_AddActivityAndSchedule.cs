using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityAndSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AnimalId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_Animals_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CapacityLeft = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Schedules_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Activities",
                columns: new[] { "Id", "AnimalId", "Capacity", "CreatedAt", "Description", "Duration", "ImageUrl", "Name", "Price", "Status", "Type" },
                values: new object[,]
                {
                    { 1, 1, 15, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Help keepers prepare fruit for Kavi and join a guided feeding session with our Asian elephant.", new TimeSpan(0, 1, 30, 0, 0), null, "Elephant Feeding Experience", 25.00m, 1, 0 },
                    { 2, null, 20, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "A guided evening walk through the park to spot nocturnal animals as they become active.", new TimeSpan(0, 2, 0, 0, 0), null, "Nocturnal Safari Walk", 30.00m, 1, 1 },
                    { 3, null, 12, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Learn composition and lighting tips while photographing macaws and other birds in the aviary.", new TimeSpan(0, 2, 0, 0, 0), null, "Bird Photography Workshop", 40.00m, 1, 2 },
                    { 4, null, 4, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Spend a full day with keepers caring for Bao, from breakfast bamboo to enclosure enrichment.", new TimeSpan(0, 7, 0, 0, 0), null, "Panda Keeper for a Day", 120.00m, 0, 3 }
                });

            migrationBuilder.InsertData(
                table: "Schedules",
                columns: new[] { "Id", "ActivityId", "CapacityLeft", "Date", "EndTime", "StartTime", "Status" },
                values: new object[,]
                {
                    { 1, 1, 15, new DateOnly(2026, 9, 9), new TimeOnly(10, 30, 0), new TimeOnly(9, 0, 0), 0 },
                    { 2, 3, 12, new DateOnly(2026, 9, 9), new TimeOnly(13, 0, 0), new TimeOnly(11, 0, 0), 0 },
                    { 3, 1, 15, new DateOnly(2026, 9, 10), new TimeOnly(10, 30, 0), new TimeOnly(9, 0, 0), 0 },
                    { 4, 2, 20, new DateOnly(2026, 9, 10), new TimeOnly(22, 0, 0), new TimeOnly(20, 0, 0), 0 },
                    { 5, 3, 12, new DateOnly(2026, 9, 11), new TimeOnly(12, 0, 0), new TimeOnly(10, 0, 0), 0 },
                    { 6, 1, 15, new DateOnly(2026, 9, 12), new TimeOnly(15, 30, 0), new TimeOnly(14, 0, 0), 0 },
                    { 7, 2, 20, new DateOnly(2026, 9, 12), new TimeOnly(21, 30, 0), new TimeOnly(19, 30, 0), 0 },
                    { 8, 2, 20, new DateOnly(2026, 9, 13), new TimeOnly(22, 0, 0), new TimeOnly(20, 0, 0), 0 },
                    { 9, 3, 12, new DateOnly(2026, 9, 13), new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_AnimalId",
                table: "Activities",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_ActivityId",
                table: "Schedules",
                column: "ActivityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Schedules");

            migrationBuilder.DropTable(
                name: "Activities");
        }
    }
}
