using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveReservationUserScheduleIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_UserId",
                table: "Reservations");

            migrationBuilder.Sql(
                """
                DECLARE @Duplicates TABLE (
                    Id int NOT NULL PRIMARY KEY,
                    ScheduleId int NOT NULL,
                    GuestCount int NOT NULL
                );

                ;WITH Ranked AS (
                    SELECT
                        r.Id,
                        r.ScheduleId,
                        r.GuestCount,
                        ROW_NUMBER() OVER (
                            PARTITION BY r.UserId, r.ScheduleId
                            ORDER BY
                                CASE WHEN p.Status = 1 THEN 0 ELSE 1 END,
                                r.CreatedAt,
                                r.Id
                        ) AS RowRank
                    FROM Reservations r
                    LEFT JOIN Payments p ON p.Id = r.PaymentId
                    WHERE r.Status <> 3
                )
                INSERT INTO @Duplicates (Id, ScheduleId, GuestCount)
                SELECT Id, ScheduleId, GuestCount
                FROM Ranked
                WHERE RowRank > 1;

                UPDATE r
                SET
                    Status = 3,
                    CancelledAt = SYSUTCDATETIME(),
                    CancellationReason = N'Cancelled because this user already has another booking for the same schedule.'
                FROM Reservations r
                INNER JOIN @Duplicates d ON d.Id = r.Id;

                UPDATE s
                SET CapacityLeft = CASE
                    WHEN s.CapacityLeft + extra.Released > a.Capacity THEN a.Capacity
                    ELSE s.CapacityLeft + extra.Released
                END
                FROM Schedules s
                INNER JOIN Activities a ON a.Id = s.ActivityId
                INNER JOIN (
                    SELECT ScheduleId, SUM(GuestCount) AS Released
                    FROM @Duplicates
                    GROUP BY ScheduleId
                ) extra ON extra.ScheduleId = s.Id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_UserId_ScheduleId_Active",
                table: "Reservations",
                columns: new[] { "UserId", "ScheduleId" },
                unique: true,
                filter: "[Status] <> 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_UserId_ScheduleId_Active",
                table: "Reservations");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_UserId",
                table: "Reservations",
                column: "UserId");
        }
    }
}
