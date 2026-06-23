using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class SwitchQuestionCategoriesToIdKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Detach questions from the name-based category key.
            migrationBuilder.DropForeignKey(
                name: "FK_question_definitions_question_categories_Category",
                table: "question_definitions");

            migrationBuilder.DropIndex(
                name: "IX_question_definitions_Category_IsEnabled",
                table: "question_definitions");

            // 2. Give every category a stable surrogate id.
            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "question_categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE question_categories SET \"Id\" = gen_random_uuid();");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "question_categories",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropPrimaryKey(
                name: "PK_question_categories",
                table: "question_categories");

            migrationBuilder.AddPrimaryKey(
                name: "PK_question_categories",
                table: "question_categories",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_question_categories_Name",
                table: "question_categories",
                column: "Name",
                unique: true);

            // 3. Point questions at the category id.
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "question_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE question_definitions AS qd
                SET "CategoryId" = qc."Id"
                FROM question_categories AS qc
                WHERE qd."Category" = qc."Name";
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "question_definitions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // 4. Rename the seeded machine-coded categories to readable names.
            migrationBuilder.Sql("""
                UPDATE question_categories
                SET "Name" = CASE "Name"
                        WHEN 'lore' THEN 'Лор'
                        WHEN 'locations' THEN 'Локации'
                        WHEN 'weapons_and_items' THEN 'Оружие и предметы'
                        WHEN 'stats' THEN 'Статистика'
                        ELSE "Name"
                    END,
                    "UpdatedAtUtc" = CURRENT_TIMESTAMP
                WHERE "Name" IN ('lore', 'locations', 'weapons_and_items', 'stats');
                """);

            // 5. Drop the obsolete name-based category column and wire the new FK.
            migrationBuilder.DropColumn(
                name: "Category",
                table: "question_definitions");

            migrationBuilder.CreateIndex(
                name: "IX_question_definitions_CategoryId_IsEnabled",
                table: "question_definitions",
                columns: new[] { "CategoryId", "IsEnabled" });

            migrationBuilder.AddForeignKey(
                name: "FK_question_definitions_question_categories_CategoryId",
                table: "question_definitions",
                column: "CategoryId",
                principalTable: "question_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_question_definitions_question_categories_CategoryId",
                table: "question_definitions");

            migrationBuilder.DropIndex(
                name: "IX_question_definitions_CategoryId_IsEnabled",
                table: "question_definitions");

            // Restore the name-based category column.
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "question_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            // Revert seeded readable names back to machine codes.
            migrationBuilder.Sql("""
                UPDATE question_categories
                SET "Name" = CASE "Name"
                        WHEN 'Лор' THEN 'lore'
                        WHEN 'Локации' THEN 'locations'
                        WHEN 'Оружие и предметы' THEN 'weapons_and_items'
                        WHEN 'Статистика' THEN 'stats'
                        ELSE "Name"
                    END
                WHERE "Name" IN ('Лор', 'Локации', 'Оружие и предметы', 'Статистика');
                """);

            migrationBuilder.Sql("""
                UPDATE question_definitions AS qd
                SET "Category" = qc."Name"
                FROM question_categories AS qc
                WHERE qd."CategoryId" = qc."Id";
                """);

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "question_definitions");

            migrationBuilder.DropIndex(
                name: "IX_question_categories_Name",
                table: "question_categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_question_categories",
                table: "question_categories");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "question_categories");

            migrationBuilder.AddPrimaryKey(
                name: "PK_question_categories",
                table: "question_categories",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_question_definitions_Category_IsEnabled",
                table: "question_definitions",
                columns: new[] { "Category", "IsEnabled" });

            migrationBuilder.AddForeignKey(
                name: "FK_question_definitions_question_categories_Category",
                table: "question_definitions",
                column: "Category",
                principalTable: "question_categories",
                principalColumn: "Name",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
