using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAnimalWhispererBadge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [UserBadges] AS ub
                    INNER JOIN [Badges] AS b ON b.[Id] = ub.[BadgeId]
                    WHERE b.[Criteria] = 3)
                BEGIN
                    THROW 50001, N'Animal Whisperer cannot be removed because a user holds it.', 1;
                END;

                DELETE FROM [Badges] WHERE [Criteria] = 3;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Badges",
                columns: new[] { "Id", "Criteria", "Description", "IconUrl", "Name" },
                values: new object[] { 4, 3, "Visit 10 animal profiles", null, "Animal Whisperer" });
        }
    }
}
