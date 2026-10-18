using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminDecisionAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Reservations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Reservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledByUserId",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "Reservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfirmedByUserId",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "Adoptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectedByUserId",
                table: "Adoptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Adoptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CancelledByUserId",
                table: "Reservations",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ConfirmedByUserId",
                table: "Reservations",
                column: "ConfirmedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Adoptions_RejectedByUserId",
                table: "Adoptions",
                column: "RejectedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Adoptions_Users_RejectedByUserId",
                table: "Adoptions",
                column: "RejectedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Users_CancelledByUserId",
                table: "Reservations",
                column: "CancelledByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Users_ConfirmedByUserId",
                table: "Reservations",
                column: "ConfirmedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Adoptions_Users_RejectedByUserId",
                table: "Adoptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Users_CancelledByUserId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Users_ConfirmedByUserId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_CancelledByUserId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_ConfirmedByUserId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Adoptions_RejectedByUserId",
                table: "Adoptions");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ConfirmedByUserId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "Adoptions");

            migrationBuilder.DropColumn(
                name: "RejectedByUserId",
                table: "Adoptions");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Adoptions");
        }
    }
}
