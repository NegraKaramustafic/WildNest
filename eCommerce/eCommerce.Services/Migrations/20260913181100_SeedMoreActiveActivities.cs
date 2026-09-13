using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using eCommerce.Services.Database;

#nullable disable

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ECommerceDbContext))]
    [Migration("20260913181100_SeedMoreActiveActivities")]
    public class SeedMoreActiveActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET IDENTITY_INSERT [Activities] ON;
                INSERT INTO [Activities] ([Id], [AnimalId], [Capacity], [CreatedAt], [Description], [Duration], [ImageUrl], [Name], [Price], [Status], [Type])
                VALUES
                (5, 2, 12, '2026-03-09T00:00:00.0000000Z', N'Join keepers as they prepare fruit and bamboo snacks for Zara and watch her forage in the treetops.', '01:00:00', NULL, N'Red Panda Snack Time', 20.00, 1, 0),
                (6, 3, 10, '2026-03-09T00:00:00.0000000Z', N'Meet Milo and his troop at sunrise, then help scatter grapes and browse during their morning forage.', '01:00:00', NULL, N'Lemur Morning Meet', 18.00, 1, 3),
                (7, 4, 16, '2026-03-09T00:00:00.0000000Z', N'A quiet late-afternoon walk to Luna''s mountain habitat as she becomes active on the rocky ledges.', '01:30:00', NULL, N'Snow Leopard Twilight Safari', 35.00, 1, 1),
                (8, 7, 14, '2026-03-09T00:00:00.0000000Z', N'Guided adventure through the savanna habitat with a close-up look at Nala and her pride''s daily routine.', '02:00:00', NULL, N'Lion Savanna Adventure', 45.00, 1, 4),
                (9, 6, 10, '2026-03-09T00:00:00.0000000Z', N'Learn how scarlet macaws communicate, then practice call-and-response with Rio in the aviary.', '01:30:00', NULL, N'Macaw Voice Workshop', 28.00, 1, 2),
                (10, 5, 8, '2026-03-09T00:00:00.0000000Z', N'Help design and place puzzle feeders for Bao, then watch him work out how to reach the bamboo.', '01:00:00', NULL, N'Panda Enrichment Hour', 32.00, 1, 3);
                SET IDENTITY_INSERT [Activities] OFF;
                """);

            migrationBuilder.Sql("""
                SET IDENTITY_INSERT [Schedules] ON;
                INSERT INTO [Schedules] ([Id], [ActivityId], [CapacityLeft], [Date], [EndTime], [StartTime], [Status])
                VALUES
                (10, 5, 12, '2026-09-14', '11:00:00', '10:00:00', 0),
                (11, 6, 10, '2026-09-14', '10:00:00', '09:00:00', 0),
                (12, 7, 16, '2026-09-14', '18:30:00', '17:00:00', 0),
                (13, 8, 14, '2026-09-15', '13:00:00', '11:00:00', 0),
                (14, 9, 10, '2026-09-15', '15:30:00', '14:00:00', 0),
                (15, 10, 8, '2026-09-16', '11:00:00', '10:00:00', 0);
                SET IDENTITY_INSERT [Schedules] OFF;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [Schedules] WHERE [Id] IN (10, 11, 12, 13, 14, 15);");
            migrationBuilder.Sql("DELETE FROM [Activities] WHERE [Id] IN (5, 6, 7, 8, 9, 10);");
        }
    }
}
