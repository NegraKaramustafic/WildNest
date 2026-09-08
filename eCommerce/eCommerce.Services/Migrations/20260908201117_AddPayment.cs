using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class AddPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentId",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentId",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Method = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StripePaymentIntentId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_PaymentId",
                table: "Tickets",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_PaymentId",
                table: "Reservations",
                column: "PaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Payments_PaymentId",
                table: "Reservations",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Payments_PaymentId",
                table: "Tickets",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Payments_PaymentId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Payments_PaymentId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_PaymentId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_PaymentId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "Reservations");
        }
    }
}
