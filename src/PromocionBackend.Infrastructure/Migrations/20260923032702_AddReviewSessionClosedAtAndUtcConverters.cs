using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PromocionBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewSessionClosedAtAndUtcConverters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "ReviewSessions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "ReviewSessions");
        }
    }
}
