using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PromocionBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Identification = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TeacherId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromotionProcesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionProcesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionProcesses_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeacherSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentPosition = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrentPositionStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromPosition = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ToPosition = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EligibilityJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CpDecisionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Applications_PromotionProcesses_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "PromotionProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Applications_Users_TeacherUserId",
                        column: x => x.TeacherUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcessRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromPosition = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ToPosition = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MinYearsInPosition = table.Column<int>(type: "int", nullable: false),
                    MinPublications = table.Column<int>(type: "int", nullable: false),
                    MinPublicationsInOtherLanguage = table.Column<int>(type: "int", nullable: false),
                    MinEvaluationScorePct = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MinTrainingHours = table.Column<int>(type: "int", nullable: false),
                    TrainingWindowYears = table.Column<int>(type: "int", nullable: false),
                    MinPedagogicalTrainingPct = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MinGivenTrainingHours = table.Column<int>(type: "int", nullable: true),
                    MinProjectMonths = table.Column<int>(type: "int", nullable: true),
                    ProjectRoleScope = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApplyRoleMultipliers = table.Column<bool>(type: "bit", nullable: false),
                    MinInternationalProjects = table.Column<int>(type: "int", nullable: true),
                    MinDoctoralTheses = table.Column<int>(type: "int", nullable: true),
                    MinDoctoralThesesInRank = table.Column<int>(type: "int", nullable: true),
                    RequiredLanguageLevel = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessRequirements_PromotionProcesses_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "PromotionProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Appeals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appeals_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExternalItemId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DocumentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationItems_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Stage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ReviewerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewerRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationReviews_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationReviews_Users_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_ApplicationId",
                table: "Appeals",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationItems_ApplicationId_ItemType_ExternalItemId",
                table: "ApplicationItems",
                columns: new[] { "ApplicationId", "ItemType", "ExternalItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationReviews_ApplicationId",
                table: "ApplicationReviews",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationReviews_ReviewerUserId",
                table: "ApplicationReviews",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_ProcessId_TeacherUserId",
                table: "Applications",
                columns: new[] { "ProcessId", "TeacherUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_Status",
                table: "Applications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_TeacherUserId",
                table: "Applications",
                column: "TeacherUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessRequirements_ProcessId_FromPosition",
                table: "ProcessRequirements",
                columns: new[] { "ProcessId", "FromPosition" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionProcesses_CreatedByUserId",
                table: "PromotionProcesses",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSnapshots_UserId",
                table: "TeacherSnapshots",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appeals");

            migrationBuilder.DropTable(
                name: "ApplicationItems");

            migrationBuilder.DropTable(
                name: "ApplicationReviews");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "ProcessRequirements");

            migrationBuilder.DropTable(
                name: "TeacherSnapshots");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "PromotionProcesses");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
