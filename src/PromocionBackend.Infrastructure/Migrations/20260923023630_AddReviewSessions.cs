using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PromocionBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReviewSessionId",
                table: "ApplicationReviews",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReviewSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CommissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommissionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CommissionIsPrincipal = table.Column<bool>(type: "bit", nullable: false),
                    FacultyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FacultyName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewSessions_Commissions_CommissionId",
                        column: x => x.CommissionId,
                        principalTable: "Commissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReviewSessions_PromotionProcesses_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "PromotionProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReviewSessions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationReviews_ReviewSessionId",
                table: "ApplicationReviews",
                column: "ReviewSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewSessions_CommissionId",
                table: "ReviewSessions",
                column: "CommissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewSessions_CreatedByUserId",
                table: "ReviewSessions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewSessions_ProcessId_Type_FacultyId_CreatedAt",
                table: "ReviewSessions",
                columns: new[] { "ProcessId", "Type", "FacultyId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationReviews_ReviewSessions_ReviewSessionId",
                table: "ApplicationReviews",
                column: "ReviewSessionId",
                principalTable: "ReviewSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationReviews_ReviewSessions_ReviewSessionId",
                table: "ApplicationReviews");

            migrationBuilder.DropTable(
                name: "ReviewSessions");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationReviews_ReviewSessionId",
                table: "ApplicationReviews");

            migrationBuilder.DropColumn(
                name: "ReviewSessionId",
                table: "ApplicationReviews");
        }
    }
}
