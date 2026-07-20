using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedTestUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var timestamp = new DateTime(2026, 7, 17, 16, 28, 16, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                table: "users",
                columns: new[]
                {
                    "Id",
                    "TwitchUserId",
                    "Login",
                    "DisplayName",
                    "Email",
                    "EmailVerified",
                    "ProfileImageUrl",
                    "BroadcasterType",
                    "TwitchUserType",
                    "IsActive",
                    "LastLoginAtUtc",
                    "CreatedAtUtc",
                    "UpdatedAtUtc"
                },
                values: new object[,]
                {
                    {
                        Guid.Parse("4f00c7f1-08e2-4d2e-b27d-7a943b5740c1"),
                        "test-user-001",
                        "anna_sokolova",
                        "Anna Sokolova",
                        null,
                        null,
                        null,
                        null,
                        null,
                        true,
                        null,
                        timestamp,
                        timestamp
                    },
                    {
                        Guid.Parse("13f1a25d-227b-4e3d-a6e6-0a4d83b5cbb2"),
                        "test-user-002",
                        "dmitry_volkov",
                        "Dmitry Volkov",
                        null,
                        null,
                        null,
                        null,
                        null,
                        true,
                        null,
                        timestamp,
                        timestamp
                    },
                    {
                        Guid.Parse("0dc2383c-dde8-46ad-8f21-00f1430b7c31"),
                        "test-user-003",
                        "maria_orlova",
                        "Maria Orlova",
                        null,
                        null,
                        null,
                        null,
                        null,
                        true,
                        null,
                        timestamp,
                        timestamp
                    },
                    {
                        Guid.Parse("2dc6119a-2693-4449-8fbf-2b77c9c69bf5"),
                        "test-user-004",
                        "ivan_petrov",
                        "Ivan Petrov",
                        null,
                        null,
                        null,
                        null,
                        null,
                        true,
                        null,
                        timestamp,
                        timestamp
                    },
                    {
                        Guid.Parse("672bd1cc-4e79-4d3c-a35f-f0ce0b3779b0"),
                        "test-user-005",
                        "elena_morozova",
                        "Elena Morozova",
                        null,
                        null,
                        null,
                        null,
                        null,
                        true,
                        null,
                        timestamp,
                        timestamp
                    },
                    {
                        Guid.Parse("59a208a4-22ac-4afb-b7ab-9186bb25d788"),
                        "test-user-006",
                        "maxim_lebedev",
                        "Maxim Lebedev",
                        null,
                        null,
                        null,
                        null,
                        null,
                        true,
                        null,
                        timestamp,
                        timestamp
                    },
                    {
                        Guid.Parse("e0b67312-f6d7-44d9-a0f9-9d8e53810b86"),
                        "test-user-007",
                        "olga_nikitina",
                        "Olga Nikitina",
                        null,
                        null,
                        null,
                        null,
                        null,
                        true,
                        null,
                        timestamp,
                        timestamp
                    },
                    {
                        Guid.Parse("f025fa80-cbf6-46ee-a4d5-b44b3dfb9182"),
                        "test-user-008",
                        "sergey_kuznetsov",
                        "Sergey Kuznetsov",
                        null,
                        null,
                        null,
                        null,
                        null,
                        true,
                        null,
                        timestamp,
                        timestamp
                    },
                    {
                        Guid.Parse("9e4dac78-17d7-4096-a8a2-033c16085560"),
                        "test-user-009",
                        "natalia_romanova",
                        "Natalia Romanova",
                        null,
                        null,
                        null,
                        null,
                        null,
                        true,
                        null,
                        timestamp,
                        timestamp
                    },
                    {
                        Guid.Parse("ac84f417-6828-43e3-9294-2eb9bb9156c6"),
                        "test-user-010",
                        "artem_fedorov",
                        "Artem Fedorov",
                        null,
                        null,
                        null,
                        null,
                        null,
                        true,
                        null,
                        timestamp,
                        timestamp
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    Guid.Parse("4f00c7f1-08e2-4d2e-b27d-7a943b5740c1"),
                    Guid.Parse("13f1a25d-227b-4e3d-a6e6-0a4d83b5cbb2"),
                    Guid.Parse("0dc2383c-dde8-46ad-8f21-00f1430b7c31"),
                    Guid.Parse("2dc6119a-2693-4449-8fbf-2b77c9c69bf5"),
                    Guid.Parse("672bd1cc-4e79-4d3c-a35f-f0ce0b3779b0"),
                    Guid.Parse("59a208a4-22ac-4afb-b7ab-9186bb25d788"),
                    Guid.Parse("e0b67312-f6d7-44d9-a0f9-9d8e53810b86"),
                    Guid.Parse("f025fa80-cbf6-46ee-a4d5-b44b3dfb9182"),
                    Guid.Parse("9e4dac78-17d7-4096-a8a2-033c16085560"),
                    Guid.Parse("ac84f417-6828-43e3-9294-2eb9bb9156c6")
                });
        }
    }
}
