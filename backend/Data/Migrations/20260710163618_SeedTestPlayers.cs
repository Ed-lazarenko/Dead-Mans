using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace backend.Data.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedTestPlayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "BroadcasterType", "CreatedAtUtc", "DisplayName", "Email", "EmailVerified", "IsActive", "LastLoginAtUtc", "Login", "ProfileImageUrl", "TwitchUserId", "TwitchUserType", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), null, new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Test Player 1", null, null, true, null, "test_player_1", null, "test-player-1", null, new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000002"), null, new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Test Player 2", null, null, true, null, "test_player_2", null, "test-player-2", null, new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000003"), null, new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Test Player 3", null, null, true, null, "test_player_3", null, "test-player-3", null, new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000004"), null, new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Test Player 4", null, null, true, null, "test_player_4", null, "test-player-4", null, new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "user_roles",
                columns: new[] { "RoleId", "UserId", "AssignedAtUtc", "AssignedByUserId", "ExpiresAtUtc" },
                values: new object[,]
                {
                    { (short)1, new Guid("20000000-0000-0000-0000-000000000001"), new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, null },
                    { (short)1, new Guid("20000000-0000-0000-0000-000000000002"), new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, null },
                    { (short)1, new Guid("20000000-0000-0000-0000-000000000003"), new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, null },
                    { (short)1, new Guid("20000000-0000-0000-0000-000000000004"), new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { (short)1, new Guid("20000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { (short)1, new Guid("20000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { (short)1, new Guid("20000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { (short)1, new Guid("20000000-0000-0000-0000-000000000004") });

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"));
        }
    }
}
