using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceActivityTypeEnumWithTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityTypes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ActivityTypes",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { ActivityTypeSeedIds.Feeding, null, "Feeding" },
                    { ActivityTypeSeedIds.Safari, null, "Safari" },
                    { ActivityTypeSeedIds.Workshop, null, "Workshop" },
                    { ActivityTypeSeedIds.Experience, null, "Experience" },
                    { ActivityTypeSeedIds.Adventure, null, "Adventure" }
                });

            migrationBuilder.AddColumn<int>(
                name: "ActivityTypeId",
                table: "Activities",
                type: "int",
                nullable: true);

            // Activities.Type stored the former enum ordinal (Feeding = 0 … Adventure = 4).
            // Seeded ActivityType ids are those ordinals plus one, so every existing row keeps its type.
            migrationBuilder.Sql(
                $"""
                UPDATE [Activities]
                SET [ActivityTypeId] = CASE [Type]
                    WHEN 0 THEN {ActivityTypeSeedIds.Feeding}
                    WHEN 1 THEN {ActivityTypeSeedIds.Safari}
                    WHEN 2 THEN {ActivityTypeSeedIds.Workshop}
                    WHEN 3 THEN {ActivityTypeSeedIds.Experience}
                    WHEN 4 THEN {ActivityTypeSeedIds.Adventure}
                    ELSE [Type] + 1
                END
                """);

            migrationBuilder.AlterColumn<int>(
                name: "ActivityTypeId",
                table: "Activities",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Activities");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ActivityTypeId",
                table: "Activities",
                column: "ActivityTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTypes_Name",
                table: "ActivityTypes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_ActivityTypes_ActivityTypeId",
                table: "Activities",
                column: "ActivityTypeId",
                principalTable: "ActivityTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_ActivityTypes_ActivityTypeId",
                table: "Activities");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Activities",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                $"""
                UPDATE [Activities]
                SET [Type] = CASE [ActivityTypeId]
                    WHEN {ActivityTypeSeedIds.Feeding} THEN 0
                    WHEN {ActivityTypeSeedIds.Safari} THEN 1
                    WHEN {ActivityTypeSeedIds.Workshop} THEN 2
                    WHEN {ActivityTypeSeedIds.Experience} THEN 3
                    WHEN {ActivityTypeSeedIds.Adventure} THEN 4
                    ELSE 0
                END
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Activities",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_Activities_ActivityTypeId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "ActivityTypeId",
                table: "Activities");

            migrationBuilder.DropTable(
                name: "ActivityTypes");
        }
    }
}
