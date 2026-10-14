using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MessageId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            // Existing rows each receive their own NEWID() value. Drop the default so
            // later inserts must supply the MessageId generated with the RabbitMQ message.
            migrationBuilder.Sql(
                """
                DECLARE @defaultName sysname;
                SELECT @defaultName = dc.name
                FROM sys.default_constraints dc
                INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                WHERE dc.parent_object_id = OBJECT_ID(N'Notifications')
                  AND c.name = N'MessageId';
                IF @defaultName IS NOT NULL
                    EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @defaultName + N']');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_MessageId",
                table: "Notifications",
                column: "MessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_MessageId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "Notifications");
        }
    }
}
