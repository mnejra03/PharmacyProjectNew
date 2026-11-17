using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Market.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdminNotificationReadStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminNotificationReadStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminUserId = table.Column<int>(type: "int", nullable: false),
                    NotificationId = table.Column<int>(type: "int", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminNotificationReadStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminNotificationReadStates_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdminNotificationReadStates_Users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotificationReadStates_AdminUserId_NotificationId",
                table: "AdminNotificationReadStates",
                columns: new[] { "AdminUserId", "NotificationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotificationReadStates_NotificationId",
                table: "AdminNotificationReadStates",
                column: "NotificationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminNotificationReadStates");
        }
    }
}
