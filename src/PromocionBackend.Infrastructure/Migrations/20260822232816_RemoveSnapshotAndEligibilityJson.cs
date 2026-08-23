using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PromocionBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSnapshotAndEligibilityJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeacherSnapshots");

            migrationBuilder.DropColumn(
                name: "EligibilityJson",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "SnapshotJson",
                table: "Applications");

            migrationBuilder.AddColumn<string>(
                name: "CurrentPosition",
                table: "Applications",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ScorePct",
                table: "Applications",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeacherId",
                table: "Applications",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TeacherName",
                table: "Applications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPosition",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ScorePct",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "TeacherName",
                table: "Applications");

            migrationBuilder.AddColumn<string>(
                name: "EligibilityJson",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SnapshotJson",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "TeacherSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentPosition = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrentPositionStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherSnapshots_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSnapshots_UserId",
                table: "TeacherSnapshots",
                column: "UserId",
                unique: true);
        }
    }
}
