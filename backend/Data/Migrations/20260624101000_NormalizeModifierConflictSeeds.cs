using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeModifierConflictSeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "modifier_conflicts",
                keyColumns: new[] { "ConflictsWithModifierId", "ModifierId" },
                keyValues: new object[] { new Guid("10000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000009") });

            migrationBuilder.DeleteData(
                table: "modifier_conflicts",
                keyColumns: new[] { "ConflictsWithModifierId", "ModifierId" },
                keyValues: new object[] { new Guid("10000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-00000000000c") });

            migrationBuilder.DeleteData(
                table: "modifier_conflicts",
                keyColumns: new[] { "ConflictsWithModifierId", "ModifierId" },
                keyValues: new object[] { new Guid("10000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-00000000000c") });

            migrationBuilder.DeleteData(
                table: "modifier_conflicts",
                keyColumns: new[] { "ConflictsWithModifierId", "ModifierId" },
                keyValues: new object[] { new Guid("10000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-00000000000d") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "modifier_conflicts",
                columns: new[] { "ConflictsWithModifierId", "ModifierId" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000009") },
                    { new Guid("10000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-00000000000c") },
                    { new Guid("10000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-00000000000c") },
                    { new Guid("10000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-00000000000d") }
                });
        }
    }
}
