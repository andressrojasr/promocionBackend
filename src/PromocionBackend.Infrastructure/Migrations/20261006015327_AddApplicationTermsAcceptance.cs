using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PromocionBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationTermsAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedAt",
                table: "Applications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermsVersion",
                table: "Applications",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TermsAcceptedAt",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "TermsVersion",
                table: "Applications");
        }
    }
}
