using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PromocionBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewLocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewLockExpiresAt",
                table: "Applications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewLockedAt",
                table: "Applications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewLockedBy",
                table: "Applications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_ReviewLockedBy_ReviewLockExpiresAt",
                table: "Applications",
                columns: new[] { "ReviewLockedBy", "ReviewLockExpiresAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Users_ReviewLockedBy",
                table: "Applications",
                column: "ReviewLockedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Users_ReviewLockedBy",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_ReviewLockedBy_ReviewLockExpiresAt",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ReviewLockExpiresAt",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ReviewLockedAt",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ReviewLockedBy",
                table: "Applications");
        }
    }
}
