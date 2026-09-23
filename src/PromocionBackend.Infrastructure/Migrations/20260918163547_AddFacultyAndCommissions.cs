using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PromocionBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFacultyAndCommissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacultyId",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacultyName",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CommissionId",
                table: "ApplicationReviews",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Commissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsPrincipal = table.Column<bool>(type: "bit", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Commissions_PromotionProcesses_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "PromotionProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Commissions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommissionMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    CargoLabel = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TeacherIdentification = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TeacherFullName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TeacherExternalId = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionMembers_Commissions_CommissionId",
                        column: x => x.CommissionId,
                        principalTable: "Commissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationReviews_CommissionId",
                table: "ApplicationReviews",
                column: "CommissionId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionMembers_CommissionId",
                table: "CommissionMembers",
                column: "CommissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_CreatedByUserId",
                table: "Commissions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_ProcessId_Type_Date",
                table: "Commissions",
                columns: new[] { "ProcessId", "Type", "Date" });

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationReviews_Commissions_CommissionId",
                table: "ApplicationReviews",
                column: "CommissionId",
                principalTable: "Commissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationReviews_Commissions_CommissionId",
                table: "ApplicationReviews");

            migrationBuilder.DropTable(
                name: "CommissionMembers");

            migrationBuilder.DropTable(
                name: "Commissions");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationReviews_CommissionId",
                table: "ApplicationReviews");

            migrationBuilder.DropColumn(
                name: "FacultyId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "FacultyName",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "CommissionId",
                table: "ApplicationReviews");
        }
    }
}
